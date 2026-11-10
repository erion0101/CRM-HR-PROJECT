using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Employment> Employments => Set<Employment>();
    public DbSet<PositionAssignment> PositionAssignments => Set<PositionAssignment>();
    public DbSet<ReportingLine> ReportingLines => Set<ReportingLine>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();
    public DbSet<CompensationRecord> CompensationRecords => Set<CompensationRecord>();
    public DbSet<EmployeeDocument> Documents => Set<EmployeeDocument>();
    public DbSet<DocumentFolder> DocumentFolders => Set<DocumentFolder>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentAssociation> DocumentAssociations => Set<DocumentAssociation>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<UserScope> UserScopes => Set<UserScope>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<AiSourceReference> AiSourceReferences => Set<AiSourceReference>();
    public DbSet<AiToolExecution> AiToolExecutions => Set<AiToolExecution>();
    public DbSet<AiUsageRecord> AiUsageRecords => Set<AiUsageRecord>();
    public DbSet<DocumentAiIndex> DocumentAiIndexes => Set<DocumentAiIndex>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();
    public DbSet<KnowledgeArticleRevision> KnowledgeArticleRevisions => Set<KnowledgeArticleRevision>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMembership> ProjectMemberships => Set<ProjectMembership>();
    public DbSet<ProjectResourceLink> ProjectResourceLinks => Set<ProjectResourceLink>();
    public DbSet<OnboardingTemplate> OnboardingTemplates => Set<OnboardingTemplate>();
    public DbSet<OnboardingTemplateVersion> OnboardingTemplateVersions => Set<OnboardingTemplateVersion>();
    public DbSet<OnboardingTemplateStep> OnboardingTemplateSteps => Set<OnboardingTemplateStep>();
    public DbSet<OnboardingAssignment> OnboardingAssignments => Set<OnboardingAssignment>();
    public DbSet<OnboardingStepProgress> OnboardingStepProgresses => Set<OnboardingStepProgress>();
    public DbSet<OnboardingAcknowledgment> OnboardingAcknowledgments => Set<OnboardingAcknowledgment>();
    public DbSet<OnboardingReadingCycle> OnboardingReadingCycles => Set<OnboardingReadingCycle>();
    public DbSet<OnboardingReadingProgress> OnboardingReadingProgresses => Set<OnboardingReadingProgress>();
    public DbSet<OnboardingQuiz> OnboardingQuizzes => Set<OnboardingQuiz>();
    public DbSet<OnboardingQuizQuestion> OnboardingQuizQuestions => Set<OnboardingQuizQuestion>();
    public DbSet<OnboardingQuizAnswerOption> OnboardingQuizAnswerOptions => Set<OnboardingQuizAnswerOption>();
    public DbSet<OnboardingQuizAttempt> OnboardingQuizAttempts => Set<OnboardingQuizAttempt>();
    public DbSet<OnboardingQuizAttemptQuestion> OnboardingQuizAttemptQuestions => Set<OnboardingQuizAttemptQuestion>();
    public DbSet<OnboardingQuizAnswer> OnboardingQuizAnswers => Set<OnboardingQuizAnswer>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("vector");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("The record was changed by another user."); }
        catch (DbUpdateException exception) when (exception.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true)
        {
            var constraint = (exception.InnerException as Npgsql.PostgresException)?.ConstraintName;
            throw new ConflictException(constraint is not null && UniqueViolationMessages.TryGetValue(constraint, out var message)
                ? message
                : "A record with the same unique value already exists.");
        }
    }

    /// <summary>Plain-language messages for the unique rules people actually hit, keyed by database constraint name.</summary>
    private static readonly IReadOnlyDictionary<string, string> UniqueViolationMessages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["IX_Employees_OrganizationId_WorkEmail"] = "This work email is already used by another employee.",
        ["IX_Employees_OrganizationId_EmployeeNumber"] = "This employee number is already used by another employee."
    };
}
