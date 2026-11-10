using System.Text.Json;
using HrManagement.Application.Common.Authorization;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Ai;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

namespace HrManagement.Infrastructure.Ai;

internal sealed class AiDataReader(AppDbContext db, IAuthorizedDataScope scope, ICurrentUserService currentUser, IEmbeddingProvider embeddings, IAiRuntimeSettings settings) : IAiDataReader
{
    private static readonly JsonSerializerOptions ToolJsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<AiToolResult> SearchEmployeesAsync(AiEmployeeSearch search, CancellationToken cancellationToken)
    {
        var allowed = await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var today = Today();
        var query = db.Employees.Where(x => x.OrganizationId == scope.OrganizationId && allowed.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var pattern = $"%{search.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.FirstName, pattern) || EF.Functions.ILike(x.LastName, pattern) ||
                EF.Functions.ILike(x.FirstName + " " + x.LastName, pattern) ||
                EF.Functions.ILike(x.EmployeeNumber, pattern) || EF.Functions.ILike(x.WorkEmail, pattern));
        }
        if (Enum.TryParse<EmploymentStatus>(search.EmploymentStatus, true, out var status))
            query = query.Where(x => db.Employments.Any(e => e.OrganizationId == scope.OrganizationId && e.EmployeeId == x.Id && e.Status == status && e.StartDate <= today && (e.EndDate == null || e.EndDate >= today)));
        if (search.OrganizationUnitId.HasValue)
            query = query.Where(x => db.PositionAssignments.Any(a => a.OrganizationId == scope.OrganizationId && a.EmployeeId == x.Id && a.DepartmentId == search.OrganizationUnitId && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)));
        if (!string.IsNullOrWhiteSpace(search.OrganizationUnit))
        {
            var organizationPattern = $"%{search.OrganizationUnit.Trim()}%";
            query = query.Where(x => db.PositionAssignments.Any(a => a.OrganizationId == scope.OrganizationId && a.EmployeeId == x.Id &&
                a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today) &&
                db.Departments.Any(d => d.Id == a.DepartmentId && d.OrganizationId == scope.OrganizationId && EF.Functions.ILike(d.Name, organizationPattern))));
        }
        if (!string.IsNullOrWhiteSpace(search.Position))
        {
            var positionPattern = $"%{search.Position.Trim()}%";
            query = query.Where(x => db.PositionAssignments.Any(a => a.OrganizationId == scope.OrganizationId && a.EmployeeId == x.Id &&
                a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today) &&
                db.Positions.Any(p => p.Id == a.PositionId && p.OrganizationId == scope.OrganizationId && EF.Functions.ILike(p.Title, positionPattern))));
        }
        if (search.ManagerEmployeeId.HasValue)
            query = query.Where(x => db.ReportingLines.Any(r => r.OrganizationId == scope.OrganizationId && r.EmployeeId == x.Id &&
                r.ManagerEmployeeId == search.ManagerEmployeeId && r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo >= today)));
        if (search.HiredFrom.HasValue)
            query = query.Where(x => db.Employments.Any(e => e.OrganizationId == scope.OrganizationId && e.EmployeeId == x.Id && e.StartDate >= search.HiredFrom));
        if (search.HiredBefore.HasValue)
            query = query.Where(x => db.Employments.Any(e => e.OrganizationId == scope.OrganizationId && e.EmployeeId == x.Id && e.StartDate < search.HiredBefore));

        var mayReadPrivateForOthers = currentUser.IsInRole(Roles.Hr);
        if (search.BirthMonth.HasValue || search.BirthDay.HasValue)
        {
            if (!mayReadPrivateForOthers)
            {
                if (currentUser.EmployeeId is not { } self) return Empty(AiResultType.EmployeeList);
                query = query.Where(x => x.Id == self);
            }
            if (search.BirthMonth.HasValue) query = query.Where(x => x.DateOfBirth != null && x.DateOfBirth.Value.Month == search.BirthMonth);
            if (search.BirthDay.HasValue) query = query.Where(x => x.DateOfBirth != null && x.DateOfBirth.Value.Day == search.BirthDay);
        }

        var rows = await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id).Take(search.Limit)
            .Select(x => new
            {
                x.Id,
                x.EmployeeNumber,
                x.FirstName,
                x.LastName,
                x.WorkEmail,
                dateOfBirth = (search.BirthMonth.HasValue || search.BirthDay.HasValue) && (mayReadPrivateForOthers || currentUser.EmployeeId == x.Id) ? x.DateOfBirth : null,
                employmentStatus = db.Employments.Where(e => e.OrganizationId == scope.OrganizationId && e.EmployeeId == x.Id && e.StartDate <= today && (e.EndDate == null || e.EndDate >= today))
                    .OrderByDescending(e => e.StartDate).Select(e => (EmploymentStatus?)e.Status).FirstOrDefault(),
                hireDate = db.Employments.Where(e => e.OrganizationId == scope.OrganizationId && e.EmployeeId == x.Id).OrderBy(e => e.StartDate).Select(e => (DateOnly?)e.StartDate).FirstOrDefault(),
                position = (from a in db.PositionAssignments join p in db.Positions on a.PositionId equals p.Id
                            where a.OrganizationId == scope.OrganizationId && a.EmployeeId == x.Id && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)
                            orderby a.EffectiveFrom descending select p.Title).FirstOrDefault(),
                organizationUnit = (from a in db.PositionAssignments join d in db.Departments on a.DepartmentId equals d.Id
                                    where a.OrganizationId == scope.OrganizationId && a.EmployeeId == x.Id && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)
                                    orderby a.EffectiveFrom descending select d.Name).FirstOrDefault()
            }).ToListAsync(cancellationToken);
        return Result(AiResultType.EmployeeList, rows, rows.Select(x => EmployeeSource(x.Id, $"{x.FirstName} {x.LastName}")));
    }

    public async Task<AiToolResult> GetEmployeeProfileAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(employeeId, cancellationToken)) return Empty(AiResultType.EmployeeDetails);
        var today = Today();
        var employee = await db.Employees.Where(x => x.OrganizationId == scope.OrganizationId && x.Id == employeeId)
            .Select(x => new { x.Id, x.EmployeeNumber, x.FirstName, x.LastName, x.WorkEmail, x.Phone, x.DateOfBirth }).SingleOrDefaultAsync(cancellationToken);
        if (employee is null) return Empty(AiResultType.EmployeeDetails);
        var employment = await db.Employments.Where(x => x.OrganizationId == scope.OrganizationId && x.EmployeeId == employeeId && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today))
            .OrderByDescending(x => x.StartDate).Select(x => new { status = x.Status.ToString(), x.StartDate, x.EndDate }).FirstOrDefaultAsync(cancellationToken);
        var hireDate = await db.Employments.Where(x => x.OrganizationId == scope.OrganizationId && x.EmployeeId == employeeId)
            .OrderBy(x => x.StartDate).Select(x => (DateOnly?)x.StartDate).FirstOrDefaultAsync(cancellationToken);
        var assignment = await (from a in db.PositionAssignments
                                join p in db.Positions on a.PositionId equals p.Id
                                join d in db.Departments on a.DepartmentId equals d.Id
                                where a.OrganizationId == scope.OrganizationId && p.OrganizationId == scope.OrganizationId &&
                                      d.OrganizationId == scope.OrganizationId && a.EmployeeId == employeeId &&
                                      a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)
                                orderby a.EffectiveFrom descending
                                select new { position = p.Title, organizationUnit = d.Name, organizationUnitId = d.Id }).FirstOrDefaultAsync(cancellationToken);
        var manager = await (from r in db.ReportingLines
                             join e in db.Employees on r.ManagerEmployeeId equals e.Id
                             where r.OrganizationId == scope.OrganizationId && e.OrganizationId == scope.OrganizationId &&
                                   r.EmployeeId == employeeId && r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo >= today)
                             orderby r.EffectiveFrom descending
                             select new { e.Id, name = e.FirstName + " " + e.LastName }).FirstOrDefaultAsync(cancellationToken);
        var mayReadPrivate = currentUser.IsInRole(Roles.Hr) || currentUser.EmployeeId == employeeId;
        var row = new { employee.Id, employee.EmployeeNumber, employee.FirstName, employee.LastName, employee.WorkEmail,
            phone = mayReadPrivate ? employee.Phone : null,
            dateOfBirth = mayReadPrivate ? employee.DateOfBirth : null,
            employmentStatus = employment?.status,
            hireDate,
            employmentEndDate = employment?.EndDate,
            position = assignment?.position,
            organizationUnit = assignment?.organizationUnit,
            manager };
        return Result(AiResultType.EmployeeDetails, row, [EmployeeSource(employee.Id, $"{employee.FirstName} {employee.LastName}")]);
    }

    public async Task<AiToolResult> GetEmployeeHistoryAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(employeeId, cancellationToken)) return Empty(AiResultType.EmployeeDetails);
        var employment = await db.Employments.Where(x => x.OrganizationId == scope.OrganizationId && x.EmployeeId == employeeId).OrderByDescending(x => x.StartDate)
            .Select(x => new { status = x.Status.ToString(), x.StartDate, x.EndDate }).ToListAsync(cancellationToken);
        var assignments = await (from a in db.PositionAssignments join p in db.Positions on a.PositionId equals p.Id join d in db.Departments on a.DepartmentId equals d.Id
                                 where a.OrganizationId == scope.OrganizationId && a.EmployeeId == employeeId orderby a.EffectiveFrom descending
                                 select new { position = p.Title, organizationUnit = d.Name, a.EffectiveFrom, a.EffectiveTo }).ToListAsync(cancellationToken);
        var reporting = await (from r in db.ReportingLines join e in db.Employees on r.ManagerEmployeeId equals e.Id
                               where r.OrganizationId == scope.OrganizationId && r.EmployeeId == employeeId orderby r.EffectiveFrom descending
                               select new { managerId = e.Id, managerName = e.FirstName + " " + e.LastName, r.EffectiveFrom, r.EffectiveTo }).ToListAsync(cancellationToken);
        return Result(AiResultType.EmployeeDetails, new { employment, assignments, reporting }, [EmployeeSource(employeeId, "Employee history")]);
    }

    public async Task<AiToolResult> GetDirectReportsAsync(Guid managerEmployeeId, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(managerEmployeeId, cancellationToken)) return Empty(AiResultType.EmployeeList);
        var allowed = await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var today = Today();
        var rows = await (from r in db.ReportingLines join e in db.Employees on r.EmployeeId equals e.Id
                          where r.OrganizationId == scope.OrganizationId && e.OrganizationId == scope.OrganizationId &&
                                r.ManagerEmployeeId == managerEmployeeId && allowed.Contains(e.Id) &&
                                r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo >= today)
                          orderby e.LastName, e.FirstName select new { e.Id, e.EmployeeNumber, name = e.FirstName + " " + e.LastName, e.WorkEmail }).ToListAsync(cancellationToken);
        return Result(AiResultType.EmployeeList, rows, rows.Select(x => EmployeeSource(x.Id, x.name)));
    }

    public async Task<AiToolResult> GetOrganizationStructureAsync(int limit, CancellationToken cancellationToken)
    {
        var allowed = await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var today = Today();
        var units = await db.Departments.Where(x => x.OrganizationId == scope.OrganizationId).OrderBy(x => x.Type).ThenBy(x => x.Name).Take(limit)
            .Select(x => new { x.Id, x.Name, x.Code, type = x.Type.ToString(), x.ParentDepartmentId,
                employeeCount = db.PositionAssignments.Count(a => allowed.Contains(a.EmployeeId) && a.DepartmentId == x.Id && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)) })
            .ToListAsync(cancellationToken);
        var positions = await db.Positions.Where(x => x.OrganizationId == scope.OrganizationId).OrderBy(x => x.Title).ThenBy(x => x.Id).Take(limit)
            .Select(x => new { x.Id, x.Title, x.Code,
                employeeCount = db.PositionAssignments.Count(a => allowed.Contains(a.EmployeeId) && a.PositionId == x.Id && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)) })
            .ToListAsync(cancellationToken);
        var sources = units.Select(x => EntitySource("OrganizationUnit", x.Id, x.Name, $"/departments"))
            .Concat(positions.Select(x => EntitySource("Position", x.Id, x.Title, "/positions")));
        return Result(AiResultType.OrganizationTree, new { units, positions }, sources);
    }

    public async Task<AiToolResult> SearchContractsAsync(AiContractSearch search, CancellationToken cancellationToken)
    {
        var allowed = await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var today = Today();
        var query = from contract in db.Contracts join employee in db.Employees on contract.EmployeeId equals employee.Id
                    where contract.OrganizationId == scope.OrganizationId && allowed.Contains(contract.EmployeeId)
                    select new { contract, employee };
        if (search.EmployeeId.HasValue) query = query.Where(x => x.contract.EmployeeId == search.EmployeeId);
        if (search.EmployeeIds is { Count: > 0 }) query = query.Where(x => search.EmployeeIds.Contains(x.contract.EmployeeId));
        if (Enum.TryParse<ContractStatus>(search.Status, true, out var status)) query = query.Where(x => x.contract.Status == status);
        if (search.StartsFrom.HasValue) query = query.Where(x => x.contract.StartDate >= search.StartsFrom);
        if (search.StartsBefore.HasValue) query = query.Where(x => x.contract.StartDate < search.StartsBefore);
        if (search.EndsFrom.HasValue) query = query.Where(x => x.contract.EndDate >= search.EndsFrom);
        if (search.EndsBefore.HasValue) query = query.Where(x => x.contract.EndDate < search.EndsBefore);
        if (search.OrganizationUnitId.HasValue)
            query = query.Where(x => db.PositionAssignments.Any(a => a.OrganizationId == scope.OrganizationId && a.EmployeeId == x.contract.EmployeeId &&
                a.DepartmentId == search.OrganizationUnitId && a.EffectiveFrom <= today && (a.EffectiveTo == null || a.EffectiveTo >= today)));
        var rows = await query.OrderBy(x => x.contract.EndDate).ThenBy(x => x.contract.Id).Take(search.Limit)
            .Select(x => new { x.contract.Id, x.contract.EmployeeId, employeeName = x.employee.FirstName + " " + x.employee.LastName,
                x.contract.ContractNumber, status = x.contract.Status.ToString(), x.contract.StartDate, x.contract.EndDate, x.contract.CurrentRevision }).ToListAsync(cancellationToken);
        return Result(AiResultType.ContractList, rows, rows.Select(x => ContractSource(x.Id, x.EmployeeId, x.ContractNumber)));
    }

    public async Task<AiToolResult> GetEmployeeContractsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(employeeId, cancellationToken)) return Empty(AiResultType.ContractList);
        var rows = await db.Contracts.Where(x => x.OrganizationId == scope.OrganizationId && x.EmployeeId == employeeId)
            .OrderByDescending(x => x.StartDate).Select(x => new { x.Id, x.ContractNumber, status = x.Status.ToString(), x.StartDate, x.EndDate, x.CurrentRevision,
                revisions = db.ContractVersions.Where(v => v.OrganizationId == scope.OrganizationId && v.ContractId == x.Id).OrderByDescending(v => v.Revision)
                    .Select(v => new { v.Revision, v.StartDate, v.EndDate }).ToArray() }).ToListAsync(cancellationToken);
        return Result(AiResultType.ContractList, rows, rows.Select(x => ContractSource(x.Id, employeeId, x.ContractNumber)));
    }

    public async Task<AiToolResult> ListEmployeeDocumentsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (!await scope.CanAccessEmployeeAsync(employeeId, cancellationToken)) return Empty(AiResultType.DocumentList);
        var visibleFolders = await VisibleFolderIdsAsync(cancellationToken);
        var rows = await (from document in db.Documents join association in db.DocumentAssociations on document.Id equals association.DocumentId
                          where document.OrganizationId == scope.OrganizationId && association.AssociationType == DocumentAssociationType.Employee &&
                                association.AssociatedEntityId == employeeId && document.Status == DocumentStatus.Clean && document.AiIndexingApproved &&
                                (document.DataClassification == DataClassification.NormalInternal || document.DataClassification == DataClassification.Private) &&
                                (document.FolderId == null || visibleFolders.Contains(document.FolderId.Value))
                          orderby document.CreatedAtUtc descending
                          select new { document.Id, document.DisplayName, document.DocumentType, status = document.Status.ToString(),
                              classification = document.DataClassification.ToString(), document.AiIndexingApproved, document.CurrentVersion }).ToListAsync(cancellationToken);
        return Result(AiResultType.DocumentList, rows, rows.Select(x => DocumentSource(x.Id, employeeId, x.DisplayName, null, null, null, null)));
    }

    public async Task<AiToolResult> SearchEmployeeDocumentsAsync(AiDocumentSearch search, CancellationToken cancellationToken)
    {
        var allowed = await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        if (search.EmployeeId.HasValue && !allowed.Contains(search.EmployeeId.Value)) return Empty(AiResultType.DocumentList);
        if (search.EmployeeIds is { Count: > 0 })
        {
            var requested = search.EmployeeIds.ToHashSet();
            allowed = allowed.Where(requested.Contains).ToArray();
            if (allowed.Count == 0) return Empty(AiResultType.DocumentList);
        }
        if (string.IsNullOrWhiteSpace(search.Search)) return Empty(AiResultType.DocumentList);
        var visibleFolders = await VisibleFolderIdsAsync(cancellationToken);
        var queryVector = (await embeddings.CreateEmbeddingsAsync([search.Search.Trim()], cancellationToken))[0];
        var rows = await HybridSearchAsync(search.Search.Trim(), queryVector, allowed, visibleFolders, search.EmployeeId, search.DocumentType, search.Limit, cancellationToken);
        return Result(AiResultType.DocumentList, rows, rows.Select(x => DocumentSource(x.DocumentId, x.EmployeeId, x.DisplayName,
            x.ChunkId, x.DocumentVersionId, x.PageNumber, x.ParagraphNumber)));
    }

    public async Task<AiToolResult> GetMyOnboardingAsync(bool progressOnly, CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is not { } employeeId) return Empty(AiResultType.Onboarding);
        var assignments = await db.OnboardingAssignments.Where(x => x.OrganizationId == scope.OrganizationId && x.EmployeeId == employeeId &&
                x.Status != OnboardingAssignmentStatus.Cancelled && x.Status != OnboardingAssignmentStatus.Superseded)
            .OrderByDescending(x => x.StartDate).Take(5)
            .Select(x => new { x.Id, x.TemplateId, x.TemplateVersionNumber, x.ProjectId, x.ManagerEmployeeId, x.MentorEmployeeId,
                x.StartDate, status = x.Status.ToString(), x.CompletedAtUtc }).ToListAsync(cancellationToken);
        var rows = new List<object>();
        foreach (var assignment in assignments)
        {
            var templateName = await db.OnboardingTemplates.Where(x => x.Id == assignment.TemplateId).Select(x => x.Name).SingleAsync(cancellationToken);
            var steps = await db.OnboardingStepProgresses.Where(x => x.AssignmentId == assignment.Id)
                .OrderBy(x => x.SectionOrder).ThenBy(x => x.StepOrder)
                .Select(x => new { x.Id, x.SectionTitle, x.Title, type = x.StepType.ToString(), x.IsRequired, x.DueDate,
                    status = x.Status.ToString(), x.ExpectedOutcome }).ToListAsync(cancellationToken);
            var requiredCount = steps.Count(x => x.IsRequired);
            var completedCount = steps.Count(x => x.IsRequired && x.status == OnboardingStepStatus.Completed.ToString());
            var progressPercent = requiredCount == 0 ? 100 : (int)Math.Round(completedCount * 100m / requiredCount);
            rows.Add(progressOnly
                ? new { assignment.Id, templateName, assignment.StartDate, assignment.status, progressPercent,
                    overdue = steps.Where(x => x.DueDate < Today() && x.status != OnboardingStepStatus.Completed.ToString()).Select(x => new { x.Id, x.Title, x.DueDate, x.status }),
                    pendingReview = steps.Where(x => x.status == OnboardingStepStatus.PendingReview.ToString()).Select(x => new { x.Id, x.Title }) }
                : new { assignment.Id, templateName, assignment.TemplateVersionNumber, assignment.ProjectId, assignment.ManagerEmployeeId,
                    assignment.MentorEmployeeId, assignment.StartDate, assignment.status, progressPercent, steps });
        }
        return Result(AiResultType.Onboarding, rows, assignments.Select(x => OnboardingSource(x.Id, employeeId, "Employee onboarding journey")));
    }

    public async Task<AiToolResult> GetAssignedProjectsAsync(CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is not { } employeeId) return Empty(AiResultType.ProjectList);
        var today = Today();
        var rows = await db.Projects.Where(x => x.OrganizationId == scope.OrganizationId &&
                (x.LeadEmployeeId == employeeId || db.ProjectMemberships.Any(m => m.ProjectId == x.Id && m.EmployeeId == employeeId &&
                    m.EffectiveFrom <= today && (m.EffectiveTo == null || m.EffectiveTo >= today))))
            .OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name, x.Purpose, status = x.Status.ToString(),
                x.LeadEmployeeId, x.OnboardingContactEmployeeId }).ToListAsync(cancellationToken);
        return Result(AiResultType.ProjectList, rows, rows.Select(x => ProjectSource(x.Id, employeeId, x.Name)));
    }

    public async Task<AiToolResult> GetProjectOverviewAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await CanAccessProjectAsync(projectId, cancellationToken)) return Empty(AiResultType.ProjectList);
        var row = await db.Projects.Where(x => x.OrganizationId == scope.OrganizationId && x.Id == projectId)
            .Select(x => new { x.Id, x.Code, x.Name, x.Purpose, x.Description, status = x.Status.ToString(), x.DepartmentId,
                department = x.DepartmentId == null ? null : db.Departments.Where(d => d.Id == x.DepartmentId).Select(d => d.Name).FirstOrDefault(),
                x.LeadEmployeeId, lead = db.Employees.Where(e => e.Id == x.LeadEmployeeId).Select(e => e.FirstName + " " + e.LastName).FirstOrDefault(),
                x.OnboardingContactEmployeeId, onboardingContact = x.OnboardingContactEmployeeId == null ? null : db.Employees.Where(e => e.Id == x.OnboardingContactEmployeeId).Select(e => e.FirstName + " " + e.LastName).FirstOrDefault() })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? Empty(AiResultType.ProjectList) : Result(AiResultType.ProjectList, row,
            [ProjectSource(row.Id, currentUser.EmployeeId, row.Name)]);
    }

    public async Task<AiToolResult> GetProjectTeamAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await CanAccessProjectAsync(projectId, cancellationToken)) return Empty(AiResultType.ProjectList);
        var today = Today();
        var rows = await (from membership in db.ProjectMemberships
                          join employee in db.Employees on membership.EmployeeId equals employee.Id
                          where membership.OrganizationId == scope.OrganizationId && membership.ProjectId == projectId &&
                                membership.EffectiveFrom <= today && (membership.EffectiveTo == null || membership.EffectiveTo >= today)
                          orderby employee.FirstName, employee.LastName
                          select new { employee.Id, name = employee.FirstName + " " + employee.LastName, membership.ProjectRole,
                              membership.Responsibilities, membership.EffectiveFrom }).ToListAsync(cancellationToken);
        return Result(AiResultType.ProjectList, rows, [ProjectSource(projectId, currentUser.EmployeeId, "Project team")]);
    }

    public async Task<AiToolResult> SearchKnowledgeAsync(AiKnowledgeSearch search, CancellationToken cancellationToken)
    {
        if (search.ProjectId is { } projectId && !await CanAccessProjectAsync(projectId, cancellationToken)) return Empty(AiResultType.KnowledgeList);
        var articles = await db.KnowledgeArticles.Where(x => x.OrganizationId == scope.OrganizationId && x.ProjectId == search.ProjectId &&
            x.Status == KnowledgeArticleStatus.Published && x.PublishedRevision != null).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        var authorizedArticles = new List<KnowledgeArticle>();
        foreach (var article in articles) if (await CanReadKnowledgeAsync(article, cancellationToken)) authorizedArticles.Add(article);
        if (authorizedArticles.Count == 0) return Empty(AiResultType.KnowledgeList);
        var authorizedIds = authorizedArticles.Select(x => x.Id).ToArray();
        var pattern = $"%{search.Search.Trim()}%";
        var candidates = await (from article in db.KnowledgeArticles
                                join revision in db.KnowledgeArticleRevisions
                                    on new { ArticleId = article.Id, Revision = article.PublishedRevision }
                                    equals new { revision.ArticleId, Revision = (int?)revision.RevisionNumber }
                                where article.OrganizationId == scope.OrganizationId && article.Status == KnowledgeArticleStatus.Published && article.PublishedRevision != null &&
                                      authorizedIds.Contains(article.Id) &&
                                      (EF.Functions.ILike(revision.Title, pattern) || EF.Functions.ILike(revision.Description, pattern) ||
                                       EF.Functions.ILike(revision.Category, pattern) || EF.Functions.ILike(revision.Content, pattern))
                                orderby article.SortOrder, revision.Title
                                select new { article, revision }).Take(Math.Max(search.Limit * 3, search.Limit)).ToListAsync(cancellationToken);
        var rows = new List<object>();
        var evidence = new List<AiEvidence>();
        foreach (var candidate in candidates)
        {
            if (!await CanReadKnowledgeAsync(candidate.article, cancellationToken)) continue;
            rows.Add(new { candidate.article.Id, candidate.article.Slug, candidate.revision.Title, candidate.revision.Description,
                candidate.revision.Category, content = candidate.revision.Content[..Math.Min(candidate.revision.Content.Length, 6_000)],
                revision = candidate.revision.RevisionNumber, candidate.article.LastReviewedAtUtc });
            evidence.Add(KnowledgeSource(candidate.article.Id, currentUser.EmployeeId, candidate.revision.Title));
            if (rows.Count == search.Limit) break;
        }
        if (rows.Count < search.Limit)
        {
            var queryVector = (await embeddings.CreateEmbeddingsAsync([search.Search], cancellationToken))[0];
            var documentRows = await HybridKnowledgeDocumentSearchAsync(search.Search, queryVector, authorizedIds, search.Limit - rows.Count, cancellationToken);
            foreach (var document in documentRows)
            {
                rows.Add(new { document.ArticleId, document.DocumentId, document.DocumentVersionId, document.DisplayName,
                    content = document.Text, document.PageNumber, document.ParagraphNumber });
                evidence.Add(KnowledgeDocumentSource(document.ArticleId, document.DocumentId, document.DocumentVersionId,
                    document.ChunkId, document.DisplayName, document.PageNumber, document.ParagraphNumber));
            }
        }
        return Result(AiResultType.KnowledgeList, rows, evidence);
    }

    private async Task<IReadOnlyList<KnowledgeDocumentSearchRow>> HybridKnowledgeDocumentSearchAsync(string search, float[] queryVector,
        IReadOnlyCollection<Guid> articleIds, int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0 || articleIds.Count == 0) return [];
        const string sql = """
            WITH authorized AS MATERIALIZED (
                SELECT c."Id" AS chunk_id, c."DocumentId" AS document_id, c."DocumentVersionId" AS version_id,
                       a."AssociatedEntityId" AS article_id, d."DisplayName" AS display_name, c."Text" AS chunk_text,
                       c."PageNumber" AS page_number, c."ParagraphNumber" AS paragraph_number,
                       ts_rank_cd(c."SearchVector", plainto_tsquery('simple', @search)) AS lexical_score,
                       c."Embedding" <=> @embedding AS vector_distance
                FROM "DocumentChunks" c
                JOIN "Documents" d ON d."Id" = c."DocumentId"
                JOIN "DocumentVersions" v ON v."Id" = c."DocumentVersionId"
                JOIN "DocumentAiIndexes" i ON i."DocumentVersionId" = v."Id"
                JOIN "DocumentAssociations" a ON a."DocumentId" = d."Id"
                WHERE c."OrganizationId" = @organization_id
                  AND d."OrganizationId" = @organization_id
                  AND d."IsDeleted" = FALSE
                  AND d."AiIndexingApproved" = TRUE
                  AND d."Status" = @clean_status
                  AND d."DataClassification" = ANY(@classifications)
                  AND i."Status" = @processed_status
                  AND a."AssociationType" = ANY(@association_types)
                  AND a."AssociatedEntityId" = ANY(@article_ids)
                  AND v."VersionNumber" = d."CurrentVersion"
            ), lexical AS (
                SELECT chunk_id, row_number() OVER (ORDER BY lexical_score DESC, chunk_id) AS rank
                FROM authorized WHERE lexical_score > 0 ORDER BY lexical_score DESC, chunk_id LIMIT 30
            ), semantic AS (
                SELECT chunk_id, row_number() OVER (ORDER BY vector_distance, chunk_id) AS rank
                FROM authorized ORDER BY vector_distance, chunk_id LIMIT 30
            )
            SELECT a.chunk_id, a.document_id, a.version_id, a.article_id, a.display_name, a.chunk_text,
                   a.page_number, a.paragraph_number,
                   COALESCE(1.0::double precision / (60 + l.rank), 0) + COALESCE(1.0::double precision / (60 + s.rank), 0) AS score
            FROM authorized a
            LEFT JOIN lexical l ON l.chunk_id = a.chunk_id
            LEFT JOIN semantic s ON s.chunk_id = a.chunk_id
            WHERE l.rank IS NOT NULL OR s.rank IS NOT NULL
            ORDER BY score DESC, a.document_id, a.chunk_id
            LIMIT @candidate_limit;
            """;
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new NpgsqlParameter("search", search));
            command.Parameters.Add(new NpgsqlParameter("embedding", new Vector(queryVector)) { DataTypeName = "vector" });
            command.Parameters.Add(new NpgsqlParameter<Guid>("organization_id", scope.OrganizationId));
            command.Parameters.Add(new NpgsqlParameter<int>("clean_status", (int)DocumentStatus.Clean));
            command.Parameters.Add(new NpgsqlParameter<string[]>("classifications", [DataClassification.NormalInternal.ToString(), DataClassification.Private.ToString()]));
            command.Parameters.Add(new NpgsqlParameter<string>("processed_status", DocumentAiIndexStatus.Processed.ToString()));
            command.Parameters.Add(new NpgsqlParameter<int[]>("association_types", [(int)DocumentAssociationType.CompanyKnowledge, (int)DocumentAssociationType.ProjectKnowledge]));
            command.Parameters.Add(new NpgsqlParameter<Guid[]>("article_ids", articleIds.ToArray()));
            command.Parameters.Add(new NpgsqlParameter<int>("candidate_limit", Math.Min(30, limit * 4)));
            var rows = new List<KnowledgeDocumentSearchRow>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new KnowledgeDocumentSearchRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3),
                    reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    reader.IsDBNull(7) ? null : reader.GetInt32(7), reader.GetDouble(8)));
            return rows.GroupBy(x => x.DocumentId).SelectMany(group => group.Take(2)).Take(limit).ToArray();
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private async Task<bool> CanAccessProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager)) return await db.Projects.AnyAsync(x => x.OrganizationId == scope.OrganizationId && x.Id == projectId, cancellationToken);
        if (currentUser.EmployeeId is not { } employeeId) return false;
        var today = Today();
        return await db.Projects.AnyAsync(x => x.OrganizationId == scope.OrganizationId && x.Id == projectId &&
            (x.LeadEmployeeId == employeeId || db.ProjectMemberships.Any(m => m.ProjectId == projectId && m.EmployeeId == employeeId &&
                m.EffectiveFrom <= today && (m.EffectiveTo == null || m.EffectiveTo >= today))), cancellationToken);
    }

    private async Task<bool> CanReadKnowledgeAsync(KnowledgeArticle article, CancellationToken cancellationToken)
    {
        if (article.PublishedRevision is null || article.Status != KnowledgeArticleStatus.Published) return false;
        if (article.ProjectId is { } projectId) return await CanAccessProjectAsync(projectId, cancellationToken);
        return article.Audience switch
        {
            KnowledgeAudience.AllEmployees => true,
            KnowledgeAudience.Managers => currentUser.IsInRole(Roles.Manager) || currentUser.IsInRole(Roles.Hr) || currentUser.IsInRole(Roles.ProductManager),
            KnowledgeAudience.HrOnly => currentUser.IsInRole(Roles.Hr),
            KnowledgeAudience.Department when article.DepartmentId is { } departmentId && currentUser.EmployeeId is { } employeeId =>
                await db.PositionAssignments.AnyAsync(x => x.OrganizationId == scope.OrganizationId && x.EmployeeId == employeeId && x.DepartmentId == departmentId &&
                    x.EffectiveFrom <= Today() && (x.EffectiveTo == null || x.EffectiveTo >= Today()), cancellationToken),
            _ => false
        };
    }

    private async Task<IReadOnlyList<DocumentSearchRow>> HybridSearchAsync(
        string search,
        float[] queryVector,
        IReadOnlyCollection<Guid> allowedEmployees,
        IReadOnlyList<Guid> visibleFolders,
        Guid? employeeId,
        string? documentType,
        int limit,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH authorized AS MATERIALIZED (
                SELECT c."Id" AS chunk_id, c."DocumentId" AS document_id, c."DocumentVersionId" AS version_id,
                       a."AssociatedEntityId" AS employee_id, d."DisplayName" AS display_name, c."Text" AS chunk_text,
                       c."PageNumber" AS page_number, c."ParagraphNumber" AS paragraph_number,
                       ts_rank_cd(c."SearchVector", plainto_tsquery('simple', @search)) AS lexical_score,
                       c."Embedding" <=> @embedding AS vector_distance
                FROM "DocumentChunks" c
                JOIN "Documents" d ON d."Id" = c."DocumentId"
                JOIN "DocumentVersions" v ON v."Id" = c."DocumentVersionId"
                JOIN "DocumentAiIndexes" i ON i."DocumentVersionId" = v."Id"
                JOIN "DocumentAssociations" a ON a."DocumentId" = d."Id"
                WHERE c."OrganizationId" = @organization_id
                  AND d."OrganizationId" = @organization_id
                  AND d."IsDeleted" = FALSE
                  AND d."AiIndexingApproved" = TRUE
                  AND d."Status" = @clean_status
                  AND d."DataClassification" = ANY(@classifications)
                  AND i."Status" = @processed_status
                  AND a."AssociationType" = @employee_association
                  AND a."AssociatedEntityId" = ANY(@allowed_employees)
                  AND (@employee_id IS NULL OR a."AssociatedEntityId" = @employee_id)
                  AND (@document_type IS NULL OR d."DocumentType" ILIKE ('%' || @document_type || '%'))
                  AND v."VersionNumber" = d."CurrentVersion"
                  AND (d."FolderId" IS NULL OR d."FolderId" = ANY(@visible_folders))
            ), lexical AS (
                SELECT chunk_id, row_number() OVER (ORDER BY lexical_score DESC, chunk_id) AS rank
                FROM authorized
                WHERE lexical_score > 0
                ORDER BY lexical_score DESC, chunk_id
                LIMIT 30
            ), semantic AS (
                SELECT chunk_id, row_number() OVER (ORDER BY vector_distance, chunk_id) AS rank
                FROM authorized
                ORDER BY vector_distance, chunk_id
                LIMIT 30
            )
            SELECT a.chunk_id, a.document_id, a.version_id, a.employee_id, a.display_name, a.chunk_text,
                   a.page_number, a.paragraph_number,
                   COALESCE(1.0::double precision / (60 + l.rank), 0) + COALESCE(1.0::double precision / (60 + s.rank), 0) AS score
            FROM authorized a
            LEFT JOIN lexical l ON l.chunk_id = a.chunk_id
            LEFT JOIN semantic s ON s.chunk_id = a.chunk_id
            WHERE l.rank IS NOT NULL OR s.rank IS NOT NULL
            ORDER BY score DESC, a.document_id, a.chunk_id
            LIMIT @candidate_limit;
            """;

        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new NpgsqlParameter("search", search));
            var embedding = new NpgsqlParameter("embedding", new Vector(queryVector)) { DataTypeName = "vector" };
            command.Parameters.Add(embedding);
            command.Parameters.Add(new NpgsqlParameter<Guid>("organization_id", scope.OrganizationId));
            command.Parameters.Add(new NpgsqlParameter<int>("clean_status", (int)DocumentStatus.Clean));
            command.Parameters.Add(new NpgsqlParameter<string[]>("classifications", [DataClassification.NormalInternal.ToString(), DataClassification.Private.ToString()]));
            command.Parameters.Add(new NpgsqlParameter<string>("processed_status", DocumentAiIndexStatus.Processed.ToString()));
            command.Parameters.Add(new NpgsqlParameter<int>("employee_association", (int)DocumentAssociationType.Employee));
            command.Parameters.Add(new NpgsqlParameter<Guid[]>("allowed_employees", allowedEmployees.ToArray()));
            command.Parameters.Add(new NpgsqlParameter<Guid?>("employee_id", employeeId));
            command.Parameters.Add(new NpgsqlParameter<string?>("document_type", string.IsNullOrWhiteSpace(documentType) ? null : documentType.Trim()));
            command.Parameters.Add(new NpgsqlParameter<Guid[]>("visible_folders", visibleFolders.ToArray()));
            command.Parameters.Add(new NpgsqlParameter<int>("candidate_limit", Math.Min(30, Math.Max(limit * 4, limit))));
            var rows = new List<DocumentSearchRow>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new DocumentSearchRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3),
                    reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    reader.IsDBNull(7) ? null : reader.GetInt32(7), reader.GetDouble(8)));
            }
            var selected = new List<DocumentSearchRow>(limit);
            var perDocument = new Dictionary<Guid, int>();
            foreach (var row in rows)
            {
                if (perDocument.Count >= 4 && !perDocument.ContainsKey(row.DocumentId)) continue;
                if (perDocument.GetValueOrDefault(row.DocumentId) >= 2) continue;
                selected.Add(row);
                perDocument[row.DocumentId] = perDocument.GetValueOrDefault(row.DocumentId) + 1;
                if (selected.Count == limit) break;
            }
            return selected;
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private async Task<IReadOnlyList<Guid>> VisibleFolderIdsAsync(CancellationToken cancellationToken)
    {
        var roles = Roles.All.Where(currentUser.IsInRole).ToArray();
        var folders = await db.DocumentFolders.Where(x => x.OrganizationId == scope.OrganizationId).Select(x => new { x.Id, x.AllowedRoles }).ToListAsync(cancellationToken);
        return folders.Where(x => x.AllowedRoles.Any(roles.Contains)).Select(x => x.Id).ToArray();
    }

    private DateOnly Today()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.OrganizationTimeZone);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
    }

    private static AiToolResult Result<T>(AiResultType type, T data, IEnumerable<AiEvidence> sources)
    {
        var evidence = sources.ToArray();
        var count = data is System.Collections.ICollection collection ? collection.Count : evidence.Length;
        return new AiToolResult(type, JsonSerializer.SerializeToElement(data, ToolJsonOptions), evidence, count);
    }

    private static AiToolResult Empty(AiResultType type) => new(type, JsonSerializer.SerializeToElement(Array.Empty<object>(), ToolJsonOptions), [], 0);
    private static AiEvidence EmployeeSource(Guid id, string label) => EntitySource("Employee", id, label, $"/employees/{id}", id);
    private static AiEvidence ContractSource(Guid id, Guid employeeId, string label) => EntitySource("Contract", id, label, $"/employees/{employeeId}/contracts", employeeId);
    private static AiEvidence EntitySource(string type, Guid id, string label, string url, Guid? employeeId = null) =>
        new($"{type.ToLowerInvariant()}:{id}", "StructuredRecord", label, type, id, employeeId, null, null, null, null, url);
    private static AiEvidence DocumentSource(Guid id, Guid employeeId, string label, Guid? chunkId, Guid? versionId, int? page, int? paragraph) =>
        new(chunkId.HasValue ? $"document:{id}:{chunkId}" : $"document:{id}", chunkId.HasValue ? "DocumentChunk" : "Document", label, "Document", id, employeeId, id, versionId, chunkId,
            page.HasValue ? $"page {page}" : paragraph.HasValue ? $"paragraph {paragraph}" : null, $"/employees/{employeeId}/documents");
    private static AiEvidence ProjectSource(Guid id, Guid? employeeId, string label) => EntitySource("Project", id, label, $"/projects/{id}", employeeId);
    private static AiEvidence KnowledgeSource(Guid id, Guid? employeeId, string label) => EntitySource("KnowledgeArticle", id, label, $"/knowledge/articles/{id}", employeeId);
    private static AiEvidence OnboardingSource(Guid id, Guid employeeId, string label) => EntitySource("OnboardingAssignment", id, label, "/onboarding", employeeId);
    private static AiEvidence KnowledgeDocumentSource(Guid articleId, Guid documentId, Guid versionId, Guid chunkId, string label, int? page, int? paragraph) =>
        new($"document:{documentId}:{chunkId}", "DocumentChunk", label, "KnowledgeArticle", articleId, null, documentId, versionId, chunkId,
            page.HasValue ? $"page {page}" : paragraph.HasValue ? $"paragraph {paragraph}" : null, $"/knowledge/articles/{articleId}");

    private sealed record DocumentSearchRow(Guid ChunkId, Guid DocumentId, Guid DocumentVersionId, Guid EmployeeId, string DisplayName,
        string Text, int? PageNumber, int? ParagraphNumber, double Score);
    private sealed record KnowledgeDocumentSearchRow(Guid ChunkId, Guid DocumentId, Guid DocumentVersionId, Guid ArticleId,
        string DisplayName, string Text, int? PageNumber, int? ParagraphNumber, double Score);
}
