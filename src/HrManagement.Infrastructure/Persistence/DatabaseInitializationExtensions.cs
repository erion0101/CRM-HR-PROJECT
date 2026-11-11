using System.Security.Claims;
using HrManagement.Application.Common.Authorization;
using HrManagement.Domain;
using HrManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HrManagement.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role)) Ensure(await roleManager.CreateAsync(new IdentityRole<Guid>(role)));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var legacyHrRoles = new[] { "Admin", "HRAdmin", "HREmployee" };
        foreach (var user in await users.Users.ToListAsync(cancellationToken))
        {
            var assignedRoles = await users.GetRolesAsync(user);
            var rolesToRemove = assignedRoles.Where(legacyHrRoles.Contains).ToArray();
            if (rolesToRemove.Length == 0) continue;
            Ensure(await users.RemoveFromRolesAsync(user, rolesToRemove));
            if (!await users.IsInRoleAsync(user, Roles.Hr)) Ensure(await users.AddToRoleAsync(user, Roles.Hr));
            Ensure(await users.UpdateSecurityStampAsync(user));
        }

        var seed = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword)) throw new InvalidOperationException("Seed:AdminEmail and Seed:AdminPassword are required.");
        var organization = await db.Organizations.IgnoreQueryFilters().SingleOrDefaultAsync(cancellationToken);
        if (organization is null)
        {
            organization = Organization.Create(seed.OrganizationName);
            db.Organizations.Add(organization);
            await db.SaveChangesAsync(cancellationToken);
        }
        if (!await db.LeaveTypes.AnyAsync(x => x.OrganizationId == organization.Id, cancellationToken))
        {
            // Company defaults: 20 vacation days a year earned monthly (≈1.67 a month), unused days carried over until 30 June;
            // 20 sick days a year, separate from vacation, not carried over. HR can change these in Leave settings.
            db.LeaveTypes.Add(LeaveType.Create(organization.Id, "Vacation", "#2563eb", isPaid: true, requiresApproval: true,
                deductsFromBalance: true, daysPerYear: 20, LeaveAccrualMethod.Monthly, carriesOver: true, maxCarryOverDays: null,
                carryOverExpiryMonth: 6, carryOverExpiryDay: 30));
            db.LeaveTypes.Add(LeaveType.Create(organization.Id, "Sick leave", "#dc2626", isPaid: true, requiresApproval: true,
                deductsFromBalance: true, daysPerYear: 20, LeaveAccrualMethod.YearlyUpfront, carriesOver: false, maxCarryOverDays: null,
                carryOverExpiryMonth: null, carryOverExpiryDay: null));
            await db.SaveChangesAsync(cancellationToken);
        }
        if (!await db.DocumentFolders.IgnoreQueryFilters().AnyAsync(x => x.OrganizationId == organization.Id, cancellationToken))
        {
            var allRoles = Roles.All.ToArray();
            var defaults = new[]
            {
                ("Contracts", "Employment agreements and contract documents."),
                ("Employee Uploads", "Documents submitted by employees."),
                ("Personal Documents", "Personal employee records with scope-controlled access."),
                ("Signed Documents", "Completed and signed HR documents."),
                ("Training Documents", "Learning materials and training completion files."),
                ("Certificates", "Professional and training certificates."),
                ("1-1 Meeting Notes", "Records from employee and manager one-to-one meetings.")
            };
            foreach (var (name, description) in defaults)
                db.DocumentFolders.Add(DocumentFolder.Create(organization.Id, name, description, DocumentFolderScope.Global, null, allRoles, isSystemFolder: true));
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var folders = await db.DocumentFolders.IgnoreQueryFilters().Where(x => x.OrganizationId == organization.Id).ToListAsync(cancellationToken);
            foreach (var folder in folders)
            {
                var mappedRoles = (folder.IsSystemFolder
                    ? Roles.All
                    : folder.AllowedRoles.Select(role => legacyHrRoles.Contains(role, StringComparer.Ordinal) ? Roles.Hr : role)
                        .Where(Roles.All.Contains).Distinct(StringComparer.Ordinal).ToArray()).ToArray();
                if (mappedRoles.Length == 0) mappedRoles = [Roles.Hr];
                if (!folder.AllowedRoles.Order().SequenceEqual(mappedRoles.Order(), StringComparer.Ordinal))
                    folder.SetAllowedRoles(mappedRoles, DateTimeOffset.UtcNow);
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        var admin = await users.FindByEmailAsync(seed.AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser { Id = Guid.CreateVersion7(), OrganizationId = organization.Id, UserName = seed.AdminEmail.ToLowerInvariant(), Email = seed.AdminEmail.ToLowerInvariant(), EmailConfirmed = true, IsActive = true };
            Ensure(await users.CreateAsync(admin, seed.AdminPassword));
            Ensure(await users.AddToRoleAsync(admin, Roles.Hr));
            Ensure(await users.AddClaimAsync(admin, new Claim("organization_id", organization.Id.ToString())));
        }
        else if (!await users.IsInRoleAsync(admin, Roles.Hr))
        {
            Ensure(await users.AddToRoleAsync(admin, Roles.Hr));
            Ensure(await users.UpdateSecurityStampAsync(admin));
        }

        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (environment.IsDevelopment() && !string.IsNullOrWhiteSpace(seed.DemoUsersPassword))
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
            await SeedDevelopmentUsersAsync(db, users, passwordHasher, organization.Id, seed.DemoUsersPassword, cancellationToken);
        }

        foreach (var legacyRoleName in legacyHrRoles)
        {
            var legacyRole = await roleManager.FindByNameAsync(legacyRoleName);
            if (legacyRole is not null) Ensure(await roleManager.DeleteAsync(legacyRole));
        }
    }

    private static async Task SeedDevelopmentUsersAsync(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        IPasswordHasher<ApplicationUser> passwordHasher,
        Guid organizationId,
        string password,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new DemoUser("erion.employe@gmail.com", "DEMO-EMPLOYEE", "Erion", "Employee", Roles.Employee, "IT-DEPT", "IT-JUNIOR-SWE"),
            new DemoUser("erion.manger@gmail.com", "DEMO-MANAGER", "Erion", "Manager", Roles.Manager, "IT-DEPT", "IT-MANAGING-DIRECTOR"),
            new DemoUser("erion.hr@gmail.com", "DEMO-HR", "Erion", "HR", Roles.Hr, "HR-DEPT", "HR-SENIOR"),
            new DemoUser("erion.pm@gmail.com", "DEMO-PM", "Erion", "Product Manager", Roles.ProductManager, "IT-DEPT", "IT-PRODUCT-MANAGER")
        };

        var existingDepartments = await db.Departments
            .Where(x => x.OrganizationId == organizationId && x.Type == OrganizationUnitType.Department)
            .OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        var itDepartment = existingDepartments.FirstOrDefault(x => x.Code == "IT-DEPT" || x.Name == "IT")
            ?? existingDepartments.FirstOrDefault(x => x.Name.Contains("Technology"));
        var hrDepartment = existingDepartments.FirstOrDefault(x => x.Code == "HR-DEPT" || x.Name == "HR")
            ?? existingDepartments.FirstOrDefault(x => x.Name.Contains("People") || x.Name.Contains("Human"));
        itDepartment ??= existingDepartments.FirstOrDefault(x => x.Id != hrDepartment?.Id);
        hrDepartment ??= existingDepartments.FirstOrDefault(x => x.Id != itDepartment?.Id);
        if (itDepartment is null)
        {
            itDepartment = Department.Create(organizationId, "IT", "IT-DEPT", description: "Information technology, product, engineering, and operations.");
            db.Departments.Add(itDepartment);
        }
        else
        {
            itDepartment.Update("IT", "IT-DEPT", OrganizationUnitType.Department, itDepartment.ParentDepartmentId,
                itDepartment.ManagerEmployeeId, itDepartment.HrEmployeeId, itDepartment.ProductManagerEmployeeId,
                "Information technology, product, engineering, and operations.", DateTimeOffset.UtcNow);
        }
        if (hrDepartment is null)
        {
            hrDepartment = Department.Create(organizationId, "HR", "HR-DEPT", description: "People operations, employee support, and human resources.");
            db.Departments.Add(hrDepartment);
        }
        else
        {
            hrDepartment.Update("HR", "HR-DEPT", OrganizationUnitType.Department, hrDepartment.ParentDepartmentId,
                hrDepartment.ManagerEmployeeId, hrDepartment.HrEmployeeId, hrDepartment.ProductManagerEmployeeId,
                "People operations, employee support, and human resources.", DateTimeOffset.UtcNow);
        }
        await db.SaveChangesAsync(cancellationToken);

        var departments = new Dictionary<string, Department>(StringComparer.Ordinal)
        {
            ["IT-DEPT"] = itDepartment,
            ["HR-DEPT"] = hrDepartment
        };

        var positionCatalog = new[]
        {
            new PositionDefinition("IT-DEPT", "Intern Software Engineer", "IT-INTERN-SWE"),
            new PositionDefinition("IT-DEPT", "Junior Software Engineer", "IT-JUNIOR-SWE"),
            new PositionDefinition("IT-DEPT", "Mid-Level Software Engineer", "IT-MID-SWE"),
            new PositionDefinition("IT-DEPT", "Senior Software Engineer", "IT-SENIOR-SWE"),
            new PositionDefinition("IT-DEPT", "DevOps Engineer", "IT-DEVOPS"),
            new PositionDefinition("IT-DEPT", "IT Specialist", "IT-SPECIALIST"),
            new PositionDefinition("IT-DEPT", "Managing Director", "IT-MANAGING-DIRECTOR"),
            new PositionDefinition("IT-DEPT", "Product Manager", "IT-PRODUCT-MANAGER"),
            new PositionDefinition("HR-DEPT", "HR Intern", "HR-INTERN"),
            new PositionDefinition("HR-DEPT", "Junior HR", "HR-JUNIOR"),
            new PositionDefinition("HR-DEPT", "Mid-Level HR", "HR-MID"),
            new PositionDefinition("HR-DEPT", "Senior HR", "HR-SENIOR")
        };

        var positions = new Dictionary<string, Position>(StringComparer.Ordinal);
        foreach (var definition in positionCatalog)
        {
            var position = await db.Positions.SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Code == definition.Code, cancellationToken);
            if (position is null)
            {
                position = Position.Create(organizationId, departments[definition.DepartmentCode].Id, definition.Title, definition.Code);
                db.Positions.Add(position);
            }
            else if (position.DepartmentId != departments[definition.DepartmentCode].Id || position.Title != definition.Title)
            {
                position.Update(departments[definition.DepartmentCode].Id, definition.Title, definition.Code, DateTimeOffset.UtcNow);
            }
            positions[definition.Code] = position;
        }
        await db.SaveChangesAsync(cancellationToken);

        var employees = new Dictionary<string, Employee>(StringComparer.Ordinal);
        var effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var definition in definitions)
        {
            var employee = await db.Employees.SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.WorkEmail == definition.Email, cancellationToken);
            if (employee is null)
            {
                employee = Employee.Create(organizationId, definition.EmployeeNumber, definition.FirstName, definition.LastName, definition.Email, null);
                db.Employees.Add(employee);
                await db.SaveChangesAsync(cancellationToken);
            }
            employees[definition.Role] = employee;

            if (!await db.Employments.AnyAsync(x => x.OrganizationId == organizationId && x.EmployeeId == employee.Id, cancellationToken))
                db.Employments.Add(Employment.Create(organizationId, employee.Id, EmploymentStatus.Active, effectiveFrom, null));
            var expectedDepartment = departments[definition.DepartmentCode];
            var expectedPosition = positions[definition.PositionCode];
            var demoAssignments = await db.PositionAssignments
                .Where(x => x.OrganizationId == organizationId && x.EmployeeId == employee.Id).ToListAsync(cancellationToken);
            if (!demoAssignments.Any(x => x.DepartmentId == expectedDepartment.Id && x.PositionId == expectedPosition.Id && x.EffectiveTo == null))
            {
                db.PositionAssignments.RemoveRange(demoAssignments);
                db.PositionAssignments.Add(PositionAssignment.Create(organizationId, employee.Id, expectedPosition.Id, expectedDepartment.Id, effectiveFrom, null));
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        var manager = employees[Roles.Manager];
        foreach (var employee in employees.Values.Where(x => x.Id != manager.Id))
        {
            if (!await db.ReportingLines.AnyAsync(x => x.OrganizationId == organizationId && x.EmployeeId == employee.Id, cancellationToken))
                db.ReportingLines.Add(ReportingLine.Create(organizationId, employee.Id, manager.Id, effectiveFrom, null));
        }

        var hr = employees[Roles.Hr];
        var productManager = employees[Roles.ProductManager];
        if (itDepartment.ManagerEmployeeId != manager.Id || itDepartment.HrEmployeeId != hr.Id || itDepartment.ProductManagerEmployeeId != productManager.Id)
            itDepartment.Update(itDepartment.Name, itDepartment.Code, itDepartment.Type, itDepartment.ParentDepartmentId, manager.Id, hr.Id, productManager.Id,
                itDepartment.Description, DateTimeOffset.UtcNow);
        if (hrDepartment.ManagerEmployeeId != hr.Id || hrDepartment.HrEmployeeId is not null || hrDepartment.ProductManagerEmployeeId is not null)
            hrDepartment.Update(hrDepartment.Name, hrDepartment.Code, hrDepartment.Type, hrDepartment.ParentDepartmentId, hr.Id, null, null,
                hrDepartment.Description, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var definition in definitions)
        {
            var employee = employees[definition.Role];
            var user = await users.FindByEmailAsync(definition.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.CreateVersion7(),
                    OrganizationId = organizationId,
                    EmployeeId = employee.Id,
                    UserName = definition.Email,
                    Email = definition.Email,
                    EmailConfirmed = true,
                    IsActive = true
                };
                Ensure(await users.CreateAsync(user));
                user.PasswordHash = passwordHasher.HashPassword(user, password);
                Ensure(await users.UpdateAsync(user));
            }
            else
            {
                user.OrganizationId = organizationId;
                user.EmployeeId = employee.Id;
                user.IsActive = true;
                Ensure(await users.UpdateAsync(user));
            }

            var assignedRoles = await users.GetRolesAsync(user);
            if (assignedRoles.Count > 0 && (assignedRoles.Count != 1 || assignedRoles[0] != definition.Role))
                Ensure(await users.RemoveFromRolesAsync(user, assignedRoles));
            if (!await users.IsInRoleAsync(user, definition.Role)) Ensure(await users.AddToRoleAsync(user, definition.Role));

            var claims = await users.GetClaimsAsync(user);
            if (claims.Count > 0) Ensure(await users.RemoveClaimsAsync(user, claims));
            Ensure(await users.AddClaimsAsync(user,
            [
                new Claim("organization_id", organizationId.ToString()),
                new Claim("employee_id", employee.Id.ToString())
            ]));

            if (definition.Role == Roles.Hr && !await db.UserScopes.AnyAsync(x => x.OrganizationId == organizationId && x.UserId == user.Id && x.ScopeType == ScopeType.Organization, cancellationToken))
                db.UserScopes.Add(UserScope.Create(organizationId, user.Id, ScopeType.Organization, null));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record DemoUser(
        string Email,
        string EmployeeNumber,
        string FirstName,
        string LastName,
        string Role,
        string DepartmentCode,
        string PositionCode);

    private sealed record PositionDefinition(string DepartmentCode, string Title, string Code);

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
