using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HrManagement.Infrastructure.Persistence.Configurations;

internal sealed class LeaveTypeConfiguration : EntityConfigurationBase<LeaveType>
{
    public override void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Name).HasMaxLength(80);
        builder.Property(x => x.Color).HasMaxLength(7);
        builder.Property(x => x.DaysPerYear).HasPrecision(6, 2);
        builder.Property(x => x.MaxCarryOverDays).HasPrecision(6, 2);
        // Archived types stay readable so old requests keep their name; the repository filters them where needed.
        builder.HasIndex(x => new { x.OrganizationId, x.Name });
    }
}

internal sealed class PublicHolidayConfiguration : EntityConfigurationBase<PublicHoliday>
{
    public override void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.HasIndex(x => new { x.OrganizationId, x.Date });
    }
}

internal sealed class LeaveRequestConfiguration : EntityConfigurationBase<LeaveRequest>
{
    public override void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.DecisionComment).HasMaxLength(500);
        // One person's requests, and everyone's requests in a date range for the calendar and the dashboard.
        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.StartDate });
        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.StartDate, x.EndDate });
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.ApproverEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LeaveBalanceAdjustmentConfiguration : EntityConfigurationBase<LeaveBalanceAdjustment>
{
    public override void Configure(EntityTypeBuilder<LeaveBalanceAdjustment> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Days).HasPrecision(6, 2);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.HasIndex(x => new { x.OrganizationId, x.EmployeeId, x.Year });
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
