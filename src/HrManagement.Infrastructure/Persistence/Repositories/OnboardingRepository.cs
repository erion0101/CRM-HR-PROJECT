using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Features.Onboarding;
using HrManagement.Application.Features.Projects;
using HrManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence.Repositories;

internal sealed class OnboardingRepository(AppDbContext db, ICurrentUserService currentUser, IAuthorizedDataScope scope) : IOnboardingRepository
{
    private const int MaxQuizChunksPerDocument = 40;

    private Guid OrganizationId => currentUser.OrganizationId ?? throw new ForbiddenException();

    public void Add(OnboardingTemplate template) => db.OnboardingTemplates.Add(template);
    public void Add(OnboardingTemplateVersion version) => db.OnboardingTemplateVersions.Add(version);
    public void Add(OnboardingTemplateStep step) => db.OnboardingTemplateSteps.Add(step);
    public void Add(OnboardingAssignment assignment) => db.OnboardingAssignments.Add(assignment);
    public void Add(OnboardingStepProgress progress) => db.OnboardingStepProgresses.Add(progress);
    public void Add(OnboardingAcknowledgment acknowledgment) => db.OnboardingAcknowledgments.Add(acknowledgment);
    public void Add(OnboardingReadingCycle cycle) => db.OnboardingReadingCycles.Add(cycle);
    public void Add(OnboardingReadingProgress progress) => db.OnboardingReadingProgresses.Add(progress);
    public void Add(OnboardingQuiz quiz) => db.OnboardingQuizzes.Add(quiz);
    public void Add(OnboardingQuizQuestion question) => db.OnboardingQuizQuestions.Add(question);
    public void Add(OnboardingQuizAnswerOption option) => db.OnboardingQuizAnswerOptions.Add(option);
    public void Add(OnboardingQuizAttempt attempt) => db.OnboardingQuizAttempts.Add(attempt);
    public void Add(OnboardingQuizAttemptQuestion question) => db.OnboardingQuizAttemptQuestions.Add(question);
    public void Add(OnboardingQuizAnswer answer) => db.OnboardingQuizAnswers.Add(answer);
    public void RemoveTemplateSteps(IReadOnlyCollection<OnboardingTemplateStep> steps) => db.OnboardingTemplateSteps.RemoveRange(steps);

    public Task<OnboardingTemplate?> GetTemplateAsync(Guid id, CancellationToken cancellationToken) =>
        db.OnboardingTemplates.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);

    public Task<OnboardingTemplateVersion?> GetTemplateVersionAsync(Guid templateId, int version, CancellationToken cancellationToken) =>
        db.OnboardingTemplateVersions.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.TemplateId == templateId && x.VersionNumber == version, cancellationToken);

    public async Task<IReadOnlyList<OnboardingTemplateStep>> GetTemplateStepsAsync(Guid templateVersionId, CancellationToken cancellationToken) =>
        await db.OnboardingTemplateSteps.Where(x => x.OrganizationId == OrganizationId && x.TemplateVersionId == templateVersionId)
            .OrderBy(x => x.SectionOrder).ThenBy(x => x.StepOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OnboardingTemplateDto>> ListTemplatesAsync(bool publishedOnly, CancellationToken cancellationToken)
    {
        var query = db.OnboardingTemplates.AsNoTracking().Where(x => x.OrganizationId == OrganizationId);
        if (publishedOnly) query = query.Where(x => x.PublishedVersion != null && x.Status != OnboardingTemplateStatus.Retired);
        var templates = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var result = new List<OnboardingTemplateDto>();
        foreach (var template in templates)
        {
            var number = publishedOnly ? template.PublishedVersion!.Value : template.CurrentVersion;
            var versionId = await db.OnboardingTemplateVersions.Where(x => x.TemplateId == template.Id && x.VersionNumber == number)
                .Select(x => x.Id).SingleAsync(cancellationToken);
            var steps = await db.OnboardingTemplateSteps.AsNoTracking().Where(x => x.TemplateVersionId == versionId)
                .OrderBy(x => x.SectionOrder).ThenBy(x => x.StepOrder)
                .Select(x => new OnboardingTemplateStepDto(x.Id, x.SectionTitle, x.SectionOrder, x.Title, x.Instructions,
                    x.ExpectedOutcome, x.StepType, x.StepOrder, x.DueAfterDays, x.IsRequired, x.KnowledgeArticleId,
                    x.DocumentId, x.ExternalUrl, x.DependsOnStepId, x.MinimumReadingMinutes)).ToListAsync(cancellationToken);
            result.Add(new OnboardingTemplateDto(template.Id, template.Name, template.Description, template.DepartmentId, template.PositionId,
                template.Kind, template.ProjectId, template.AutoAssignToNewEmployees,
                template.DurationDays, template.Status, template.CurrentVersion, template.PublishedVersion, steps, template.Version));
        }
        return result;
    }

    public Task<OnboardingAssignment?> GetAssignmentAsync(Guid id, CancellationToken cancellationToken) =>
        db.OnboardingAssignments.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == id, cancellationToken);

    public Task<OnboardingStepProgress?> GetStepAsync(Guid assignmentId, Guid stepId, CancellationToken cancellationToken) =>
        db.OnboardingStepProgresses.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.AssignmentId == assignmentId && x.Id == stepId, cancellationToken);

    public async Task<IReadOnlyList<OnboardingStepProgress>> GetStepsAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        await db.OnboardingStepProgresses.Where(x => x.OrganizationId == OrganizationId && x.AssignmentId == assignmentId)
            .OrderBy(x => x.SectionOrder).ThenBy(x => x.StepOrder).ToListAsync(cancellationToken);

    public Task<bool> HasActiveAssignmentAsync(Guid employeeId, Guid templateId, CancellationToken cancellationToken) =>
        db.OnboardingAssignments.AnyAsync(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId && x.TemplateId == templateId &&
            x.Status != OnboardingAssignmentStatus.Cancelled && x.Status != OnboardingAssignmentStatus.Superseded, cancellationToken);

    public async Task<OnboardingAssignmentDto?> GetAssignmentDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var allowed = await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var assignment = await db.OnboardingAssignments.AsNoTracking().SingleOrDefaultAsync(
            x => x.OrganizationId == OrganizationId && x.Id == id && allowed.Contains(x.EmployeeId), cancellationToken);
        return assignment is null ? null : await MapAssignmentAsync(assignment, cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingAssignmentDto>> ListAccessibleAssignmentsAsync(Guid? employeeId, CancellationToken cancellationToken)
    {
        var allowed = await scope.GetAccessibleEmployeeIdsAsync(cancellationToken);
        var query = db.OnboardingAssignments.AsNoTracking().Where(x => x.OrganizationId == OrganizationId && allowed.Contains(x.EmployeeId));
        if (employeeId.HasValue) query = query.Where(x => x.EmployeeId == employeeId);
        var assignments = await query.OrderByDescending(x => x.StartDate).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var result = new List<OnboardingAssignmentDto>();
        foreach (var assignment in assignments) result.Add(await MapAssignmentAsync(assignment, cancellationToken));
        return result;
    }

    public async Task<OnboardingDashboardDto?> GetMyDashboardAsync(CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is not { } employeeId) return null;
        var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == employeeId, cancellationToken);
        if (employee is null) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var assignment = await db.PositionAssignments.AsNoTracking().Where(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId &&
                x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .OrderByDescending(x => x.EffectiveFrom).Select(x => new { x.DepartmentId, x.PositionId }).FirstOrDefaultAsync(cancellationToken);
        var department = assignment is null ? null : await db.Departments.Where(x => x.Id == assignment.DepartmentId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken);
        var position = assignment is null ? null : await db.Positions.Where(x => x.Id == assignment.PositionId).Select(x => x.Title).SingleOrDefaultAsync(cancellationToken);
        var projects = await VisibleProjectsForEmployeeAsync(employeeId, today, cancellationToken);
        var journeys = await ListAccessibleAssignmentsAsync(employeeId, cancellationToken);
        return new OnboardingDashboardDto(employee.Id, employee.FullName, position, department, projects, journeys);
    }

    public async Task<Guid?> GetPublishedRevisionIdAsync(Guid articleId, CancellationToken cancellationToken) =>
        await (from article in db.KnowledgeArticles
               join revision in db.KnowledgeArticleRevisions on new { ArticleId = article.Id, Revision = article.PublishedRevision } equals new { revision.ArticleId, Revision = (int?)revision.RevisionNumber }
               where article.OrganizationId == OrganizationId && article.Id == articleId && article.Status == KnowledgeArticleStatus.Published
               select (Guid?)revision.Id).SingleOrDefaultAsync(cancellationToken);

    public Task<bool> AcknowledgmentExistsAsync(Guid stepProgressId, Guid knowledgeRevisionId, CancellationToken cancellationToken) =>
        db.OnboardingAcknowledgments.AnyAsync(x => x.OrganizationId == OrganizationId && x.StepProgressId == stepProgressId && x.KnowledgeRevisionId == knowledgeRevisionId, cancellationToken);

    public Task<bool> IsDocumentEligibleAsync(Guid documentId, CancellationToken cancellationToken) =>
        db.Documents.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == documentId && x.Status == DocumentStatus.Clean &&
            db.DocumentAssociations.Any(a => a.DocumentId == documentId &&
                (a.AssociationType == DocumentAssociationType.CompanyKnowledge || a.AssociationType == DocumentAssociationType.ProjectKnowledge)), cancellationToken);

    public Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken) =>
        db.Departments.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == departmentId && !x.IsDeleted, cancellationToken);

    public Task<bool> PositionExistsAsync(Guid positionId, CancellationToken cancellationToken) =>
        db.Positions.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == positionId && !x.IsDeleted, cancellationToken);

    public Task<Guid?> GetCurrentDocumentVersionIdAsync(Guid documentId, CancellationToken cancellationToken) =>
        (from document in db.Documents
         join version in db.DocumentVersions on new { DocumentId = document.Id, VersionNumber = document.CurrentVersion }
             equals new { version.DocumentId, version.VersionNumber }
         where document.OrganizationId == OrganizationId && document.Id == documentId && document.Status == DocumentStatus.Clean
         select (Guid?)version.Id).SingleOrDefaultAsync(cancellationToken);

    public Task<OnboardingQuiz?> GetQuizAsync(Guid quizId, CancellationToken cancellationToken) =>
        db.OnboardingQuizzes.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == quizId, cancellationToken);

    public Task<OnboardingQuiz?> GetDraftQuizAsync(Guid templateVersionId, CancellationToken cancellationToken) =>
        db.OnboardingQuizzes.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.TemplateVersionId == templateVersionId && x.Status == OnboardingQuizStatus.Draft, cancellationToken);

    public async Task<IReadOnlyList<OnboardingQuiz>> GetPublishedQuizzesAsync(IReadOnlyList<Guid> templateVersionIds, CancellationToken cancellationToken)
    {
        var quizzes = new List<OnboardingQuiz>(templateVersionIds.Count);
        foreach (var versionId in templateVersionIds)
            if (await GetPublishedQuizAsync(versionId, cancellationToken) is { } quiz) quizzes.Add(quiz);
        return quizzes;
    }

    public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.Projects.AnyAsync(x => x.OrganizationId == OrganizationId && x.Id == projectId, cancellationToken);

    public Task<bool> HasActiveProjectPackageAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.OnboardingTemplates.AnyAsync(x => x.OrganizationId == OrganizationId && x.Kind == OnboardingTemplateKind.Project &&
            x.ProjectId == projectId && x.Status != OnboardingTemplateStatus.Retired, cancellationToken);

    public Task<OnboardingTemplate?> GetAssignableProjectPackageAsync(Guid projectId, CancellationToken cancellationToken) =>
        db.OnboardingTemplates.Where(x => x.OrganizationId == OrganizationId && x.Kind == OnboardingTemplateKind.Project &&
                x.ProjectId == projectId && x.PublishedVersion != null && x.Status != OnboardingTemplateStatus.Retired)
            .OrderByDescending(x => x.UpdatedAtUtc).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OnboardingTemplate>> GetAutoAssignPackagesAsync(CancellationToken cancellationToken) =>
        await db.OnboardingTemplates.Where(x => x.OrganizationId == OrganizationId && x.AutoAssignToNewEmployees)
            .OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken);

    public Task<bool> HasOpenAssignmentAsync(Guid employeeId, CancellationToken cancellationToken) =>
        db.OnboardingAssignments.AnyAsync(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId &&
            x.Status != OnboardingAssignmentStatus.Cancelled && x.Status != OnboardingAssignmentStatus.Superseded, cancellationToken);

    public Task<bool> HasJourneyWithPackageAsync(Guid employeeId, Guid templateId, CancellationToken cancellationToken) =>
        db.OnboardingAssignments.AnyAsync(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId &&
            (x.TemplateId == templateId || x.ProjectTemplateId == templateId) &&
            x.Status != OnboardingAssignmentStatus.Cancelled && x.Status != OnboardingAssignmentStatus.Superseded, cancellationToken);

    public async Task<IReadOnlyList<OnboardingAssignment>> GetOpenAssignmentsAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await db.OnboardingAssignments.Where(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId &&
                x.Status != OnboardingAssignmentStatus.Completed && x.Status != OnboardingAssignmentStatus.Cancelled &&
                x.Status != OnboardingAssignmentStatus.Superseded)
            .OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    public Task<Guid?> GetCurrentManagerIdAsync(Guid employeeId, DateOnly asOf, CancellationToken cancellationToken) =>
        db.ReportingLines.Where(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId &&
                x.EffectiveFrom <= asOf && (x.EffectiveTo == null || x.EffectiveTo >= asOf))
            .OrderByDescending(x => x.EffectiveFrom).Select(x => (Guid?)x.ManagerEmployeeId).FirstOrDefaultAsync(cancellationToken);

    public Task<OnboardingQuiz?> GetPublishedQuizAsync(Guid templateVersionId, CancellationToken cancellationToken) =>
        db.OnboardingQuizzes.Where(x => x.OrganizationId == OrganizationId && x.TemplateVersionId == templateVersionId && x.Status == OnboardingQuizStatus.Published)
            .OrderByDescending(x => x.QuizVersion).FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetNextQuizVersionAsync(Guid templateVersionId, CancellationToken cancellationToken) =>
        (await db.OnboardingQuizzes.Where(x => x.OrganizationId == OrganizationId && x.TemplateVersionId == templateVersionId)
            .MaxAsync(x => (int?)x.QuizVersion, cancellationToken) ?? 0) + 1;

    public async Task<OnboardingQuizAdminDto?> GetQuizAdminDtoAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await db.OnboardingQuizzes.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == quizId, cancellationToken);
        if (quiz is null) return null;
        var questions = await db.OnboardingQuizQuestions.AsNoTracking().Where(x => x.QuizId == quiz.Id)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        var ids = questions.Select(x => x.Id).ToArray();
        var options = await db.OnboardingQuizAnswerOptions.AsNoTracking().Where(x => ids.Contains(x.QuestionId))
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return new OnboardingQuizAdminDto(quiz.Id, quiz.TemplateVersionId, quiz.QuizVersion, quiz.Title, quiz.Status,
            quiz.GenerationSource, quiz.PassingPercentage, quiz.QuestionCount, quiz.RandomizeQuestions, quiz.RandomizeOptions,
            quiz.PublishedAtUtc, questions.Select(q => new OnboardingQuizQuestionAdminDto(q.Id, q.QuestionText, q.Explanation,
                q.Topic, q.Difficulty, q.SortOrder, q.SourceDocumentId, q.SourceDocumentVersionId, q.SourceKnowledgeRevisionId,
                q.SourceLabel, q.SourcePage, q.SourceParagraph, options.Where(o => o.QuestionId == q.Id)
                    .Select(o => new OnboardingQuizOptionAdminDto(o.Id, o.Text, o.IsCorrect, o.SortOrder)).ToArray(), q.Version)).ToArray(), quiz.Version,
            quiz.GenerationStatus, quiz.GenerationError);
    }

    public async Task<IReadOnlyList<OnboardingQuizQuestion>> GetQuizQuestionsAsync(Guid quizId, CancellationToken cancellationToken) =>
        await db.OnboardingQuizQuestions.Where(x => x.OrganizationId == OrganizationId && x.QuizId == quizId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OnboardingQuizAnswerOption>> GetQuestionOptionsAsync(Guid questionId, CancellationToken cancellationToken) =>
        await db.OnboardingQuizAnswerOptions.Where(x => x.OrganizationId == OrganizationId && x.QuestionId == questionId)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public Task<OnboardingQuizQuestion?> GetQuestionAsync(Guid quizId, Guid questionId, CancellationToken cancellationToken) =>
        db.OnboardingQuizQuestions.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.QuizId == quizId && x.Id == questionId, cancellationToken);

    public async Task<IReadOnlyList<OnboardingQuizAnswerOption>> GetQuestionOptionsAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken) =>
        await db.OnboardingQuizAnswerOptions.Where(x => x.OrganizationId == OrganizationId && questionIds.Contains(x.QuestionId))
            .OrderBy(x => x.QuestionId).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OnboardingQuizQuestion>> GetQuestionsAsync(IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken) =>
        await db.OnboardingQuizQuestions.Where(x => x.OrganizationId == OrganizationId && questionIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public void RemoveQuestionOptions(IReadOnlyCollection<OnboardingQuizAnswerOption> options) => db.OnboardingQuizAnswerOptions.RemoveRange(options);
    public void RemoveQuestion(OnboardingQuizQuestion question) => db.OnboardingQuizQuestions.Remove(question);

    public async Task<IReadOnlyList<QuizSourceExcerpt>> GetQuizSourceExcerptsAsync(Guid templateVersionId, CancellationToken cancellationToken)
    {
        var directDocumentIds = await db.OnboardingTemplateSteps.AsNoTracking()
            .Where(x => x.OrganizationId == OrganizationId && x.TemplateVersionId == templateVersionId && x.IsRequired && x.DocumentId != null)
            .Select(x => x.DocumentId!.Value).Distinct().ToArrayAsync(cancellationToken);
        var articleIds = await db.OnboardingTemplateSteps.AsNoTracking()
            .Where(x => x.OrganizationId == OrganizationId && x.TemplateVersionId == templateVersionId && x.IsRequired && x.KnowledgeArticleId != null)
            .Select(x => x.KnowledgeArticleId!.Value).Distinct().ToArrayAsync(cancellationToken);
        var articleDocumentIds = await db.DocumentAssociations.AsNoTracking()
            .Where(x => x.OrganizationId == OrganizationId && articleIds.Contains(x.AssociatedEntityId) &&
                (x.AssociationType == DocumentAssociationType.CompanyKnowledge || x.AssociationType == DocumentAssociationType.ProjectKnowledge))
            .Select(x => x.DocumentId).Distinct().ToArrayAsync(cancellationToken);
        var documentIds = directDocumentIds.Concat(articleDocumentIds).Distinct().ToArray();
        if (documentIds.Length == 0) return [];
        // Load chunks per document so that every required document contributes evidence.
        // A single ordered query with one global limit starved documents that sort late alphabetically.
        var excerpts = new List<QuizSourceExcerpt>();
        foreach (var documentId in documentIds)
        {
            excerpts.AddRange(await (from chunk in db.DocumentChunks.AsNoTracking()
                                     join document in db.Documents.AsNoTracking() on chunk.DocumentId equals document.Id
                                     join index in db.DocumentAiIndexes.AsNoTracking() on chunk.DocumentVersionId equals index.DocumentVersionId
                                     where chunk.OrganizationId == OrganizationId && chunk.DocumentId == documentId &&
                                           document.CurrentVersion == db.DocumentVersions.Where(v => v.Id == chunk.DocumentVersionId).Select(v => v.VersionNumber).First() &&
                                           document.AiIndexingApproved && index.Status == DocumentAiIndexStatus.Processed &&
                                           (document.DataClassification == DataClassification.NormalInternal || document.DataClassification == DataClassification.Private)
                                     orderby chunk.ChunkIndex
                                     select new QuizSourceExcerpt($"source:{chunk.Id}", chunk.DocumentId, chunk.DocumentVersionId,
                                         document.DisplayName, chunk.Text, chunk.PageNumber, chunk.ParagraphNumber))
                .Take(MaxQuizChunksPerDocument).ToListAsync(cancellationToken));
        }
        return excerpts;
    }

    public async Task<IReadOnlyList<OnboardingQuizSourceDto>> GetQuizSourcesAsync(Guid templateVersionId, CancellationToken cancellationToken)
    {
        var steps = await db.OnboardingTemplateSteps.AsNoTracking().Where(x => x.OrganizationId == OrganizationId &&
                x.TemplateVersionId == templateVersionId && x.IsRequired &&
                (x.StepType == OnboardingStepType.ReadDocument || x.StepType == OnboardingStepType.ReadArticle || x.StepType == OnboardingStepType.PolicyAcknowledgment))
            .OrderBy(x => x.SectionOrder).ThenBy(x => x.StepOrder).ToListAsync(cancellationToken);
        var result = new List<OnboardingQuizSourceDto>();
        foreach (var step in steps)
        {
            if (step.DocumentId is { } documentId)
            {
                var source = await (from document in db.Documents.AsNoTracking()
                                    join version in db.DocumentVersions.AsNoTracking() on new { DocumentId = document.Id, VersionNumber = document.CurrentVersion }
                                        equals new { version.DocumentId, version.VersionNumber }
                                    where document.Id == documentId && document.Status == DocumentStatus.Clean
                                    select new { document.Id, VersionId = version.Id, document.DisplayName }).SingleOrDefaultAsync(cancellationToken);
                if (source is not null) result.Add(new OnboardingQuizSourceDto(source.Id, source.VersionId, null, source.DisplayName, step.SectionTitle));
            }
            else if (step.KnowledgeArticleId is { } articleId)
            {
                var source = await (from article in db.KnowledgeArticles.AsNoTracking()
                                    join revision in db.KnowledgeArticleRevisions.AsNoTracking() on new { ArticleId = article.Id, Number = article.PublishedRevision }
                                        equals new { revision.ArticleId, Number = (int?)revision.RevisionNumber }
                                    where article.Id == articleId && article.Status == KnowledgeArticleStatus.Published
                                    select new { revision.Id, revision.Title }).SingleOrDefaultAsync(cancellationToken);
                if (source is not null) result.Add(new OnboardingQuizSourceDto(null, null, source.Id, source.Title, step.SectionTitle));
                var documentSources = await (from association in db.DocumentAssociations.AsNoTracking()
                                             join document in db.Documents.AsNoTracking() on association.DocumentId equals document.Id
                                             join documentVersion in db.DocumentVersions.AsNoTracking()
                                                 on new { DocumentId = document.Id, VersionNumber = document.CurrentVersion }
                                                 equals new { documentVersion.DocumentId, documentVersion.VersionNumber }
                                             where association.OrganizationId == OrganizationId && association.AssociatedEntityId == articleId &&
                                                   (association.AssociationType == DocumentAssociationType.CompanyKnowledge ||
                                                    association.AssociationType == DocumentAssociationType.ProjectKnowledge) &&
                                                   document.Status == DocumentStatus.Clean
                                             orderby document.DisplayName
                                             select new OnboardingQuizSourceDto(document.Id, documentVersion.Id, null,
                                                 document.DisplayName, step.SectionTitle)).ToListAsync(cancellationToken);
                result.AddRange(documentSources);
            }
        }
        return result.DistinctBy(x => new { x.DocumentId, x.DocumentVersionId, x.KnowledgeRevisionId }).ToArray();
    }

    public async Task<bool> IsQuizSourceAsync(Guid templateVersionId, Guid? documentId, Guid? documentVersionId,
        Guid? knowledgeRevisionId, CancellationToken cancellationToken)
    {
        if (documentId is { } docId && documentVersionId is { } versionId)
        {
            var directlyAssigned = await db.OnboardingTemplateSteps.AnyAsync(x => x.OrganizationId == OrganizationId &&
                x.TemplateVersionId == templateVersionId && x.DocumentId == docId, cancellationToken);
            var assignedThroughArticle = await (from step in db.OnboardingTemplateSteps
                                                join association in db.DocumentAssociations on step.KnowledgeArticleId equals association.AssociatedEntityId
                                                where step.OrganizationId == OrganizationId && step.TemplateVersionId == templateVersionId &&
                                                      association.DocumentId == docId &&
                                                      (association.AssociationType == DocumentAssociationType.CompanyKnowledge || association.AssociationType == DocumentAssociationType.ProjectKnowledge)
                                                select step.Id).AnyAsync(cancellationToken);
            return (directlyAssigned || assignedThroughArticle) &&
                   await db.DocumentVersions.AnyAsync(x => x.OrganizationId == OrganizationId && x.DocumentId == docId && x.Id == versionId, cancellationToken);
        }
        if (knowledgeRevisionId is { } revisionId)
            return await (from step in db.OnboardingTemplateSteps
                          join revision in db.KnowledgeArticleRevisions on step.KnowledgeArticleId equals revision.ArticleId
                          where step.OrganizationId == OrganizationId && step.TemplateVersionId == templateVersionId && revision.Id == revisionId
                          select step.Id).AnyAsync(cancellationToken);
        return false;
    }

    public Task<OnboardingReadingCycle?> GetActiveReadingCycleAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        db.OnboardingReadingCycles.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.AssignmentId == assignmentId && x.Status == OnboardingReadingCycleStatus.Active, cancellationToken);

    public async Task<IReadOnlyList<OnboardingReadingProgress>> GetReadingProgressesAsync(Guid cycleId, CancellationToken cancellationToken) =>
        await db.OnboardingReadingProgresses.Where(x => x.OrganizationId == OrganizationId && x.ReadingCycleId == cycleId).ToListAsync(cancellationToken);

    public Task<OnboardingReadingProgress?> GetReadingProgressAsync(Guid cycleId, Guid stepProgressId, CancellationToken cancellationToken) =>
        db.OnboardingReadingProgresses.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.ReadingCycleId == cycleId && x.StepProgressId == stepProgressId, cancellationToken);

    public async Task<(EmployeeDocument Document, DocumentVersion Version)?> GetReadingDocumentAsync(Guid cycleId, Guid stepProgressId, CancellationToken cancellationToken)
    {
        var result = await (from progress in db.OnboardingReadingProgresses.AsNoTracking()
                            join document in db.Documents.AsNoTracking() on progress.DocumentId equals document.Id
                            join version in db.DocumentVersions.AsNoTracking() on progress.DocumentVersionId equals version.Id
                            where progress.OrganizationId == OrganizationId && progress.ReadingCycleId == cycleId &&
                                  progress.StepProgressId == stepProgressId && document.Status == DocumentStatus.Clean
                            select new { document, version }).SingleOrDefaultAsync(cancellationToken);
        return result is null ? null : (result.document, result.version);
    }

    public Task<OnboardingQuizAttempt?> GetAttemptAsync(Guid attemptId, CancellationToken cancellationToken) =>
        db.OnboardingQuizAttempts.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == attemptId, cancellationToken);

    public Task<OnboardingQuizAttempt?> GetInProgressAttemptAsync(Guid assignmentId, Guid cycleId, CancellationToken cancellationToken) =>
        db.OnboardingQuizAttempts.SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.AssignmentId == assignmentId &&
            x.ReadingCycleId == cycleId && x.Status == OnboardingQuizAttemptStatus.InProgress, cancellationToken);

    public Task<int> CountAttemptsAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        db.OnboardingQuizAttempts.CountAsync(x => x.OrganizationId == OrganizationId && x.AssignmentId == assignmentId, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetLastSubmittedAttemptQuestionIdsAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        var lastAttemptId = await db.OnboardingQuizAttempts.AsNoTracking()
            .Where(x => x.OrganizationId == OrganizationId && x.AssignmentId == assignmentId && x.Status != OnboardingQuizAttemptStatus.InProgress)
            .OrderByDescending(x => x.AttemptNumber).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        if (lastAttemptId is null) return new HashSet<Guid>();
        var ids = await db.OnboardingQuizAttemptQuestions.AsNoTracking()
            .Where(x => x.OrganizationId == OrganizationId && x.AttemptId == lastAttemptId)
            .Select(x => x.QuestionId).ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlyList<OnboardingQuizAttemptQuestion>> GetAttemptQuestionsAsync(Guid attemptId, CancellationToken cancellationToken) =>
        await db.OnboardingQuizAttemptQuestions.Where(x => x.OrganizationId == OrganizationId && x.AttemptId == attemptId)
            .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public async Task<OnboardingQuizAttemptDto?> GetAttemptDtoAsync(Guid attemptId, bool includeQuestions, CancellationToken cancellationToken)
    {
        var attempt = await db.OnboardingQuizAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == OrganizationId && x.Id == attemptId, cancellationToken);
        if (attempt is null) return null;
        var cycleNumber = await db.OnboardingReadingCycles.Where(x => x.Id == attempt.ReadingCycleId).Select(x => x.CycleNumber).SingleAsync(cancellationToken);
        var questionDtos = new List<OnboardingQuizQuestionDto>();
        if (includeQuestions && attempt.Status == OnboardingQuizAttemptStatus.InProgress)
        {
            var attemptQuestions = await db.OnboardingQuizAttemptQuestions.AsNoTracking().Where(x => x.AttemptId == attempt.Id)
                .OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
            var questionIds = attemptQuestions.Select(x => x.QuestionId).ToArray();
            var questions = await db.OnboardingQuizQuestions.AsNoTracking().Where(x => questionIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
            var options = await db.OnboardingQuizAnswerOptions.AsNoTracking().Where(x => questionIds.Contains(x.QuestionId)).ToListAsync(cancellationToken);
            foreach (var item in attemptQuestions)
            {
                var question = questions[item.QuestionId];
                var byId = options.Where(x => x.QuestionId == question.Id).ToDictionary(x => x.Id);
                var ordered = item.GetOptionOrder().Where(byId.ContainsKey).Select(id => new OnboardingQuizOptionDto(id, byId[id].Text)).ToArray();
                questionDtos.Add(new OnboardingQuizQuestionDto(question.Id, question.QuestionText, item.SortOrder + 1, ordered));
            }
        }
        return new OnboardingQuizAttemptDto(attempt.Id, attempt.AssignmentId, attempt.AttemptNumber, cycleNumber, attempt.Status,
            attempt.PassingPercentage, attempt.ScorePercentage, attempt.CorrectAnswers, attempt.TotalQuestions, attempt.StartedAtUtc,
            attempt.SubmittedAtUtc, questionDtos, attempt.Version);
    }

    private async Task<OnboardingAssignmentDto> MapAssignmentAsync(OnboardingAssignment assignment, CancellationToken cancellationToken)
    {
        var employeeIds = new[] { assignment.EmployeeId, assignment.ManagerEmployeeId, assignment.MentorEmployeeId }.OfType<Guid>().Distinct().ToArray();
        var employees = await db.Employees.AsNoTracking().Where(x => employeeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName, cancellationToken);
        var templateName = await db.OnboardingTemplates.Where(x => x.Id == assignment.TemplateId).Select(x => x.Name).SingleAsync(cancellationToken);
        var projectTemplateName = assignment.ProjectTemplateId is { } projectTemplateId
            ? await db.OnboardingTemplates.Where(x => x.Id == projectTemplateId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken)
            : null;
        var projectName = assignment.ProjectId is { } projectId ? await db.Projects.Where(x => x.Id == projectId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken) : null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var steps = await db.OnboardingStepProgresses.AsNoTracking().Where(x => x.AssignmentId == assignment.Id)
            .OrderBy(x => x.SectionOrder).ThenBy(x => x.StepOrder)
            .Select(x => new OnboardingStepDto(x.Id, x.SectionTitle, x.SectionOrder, x.Title, x.Instructions, x.ExpectedOutcome,
                x.StepType, x.StepOrder, x.IsRequired, x.DueDate, x.DueDate < today && x.Status != OnboardingStepStatus.Completed,
                x.KnowledgeArticleId, x.DocumentId, x.ExternalUrl, x.Status, x.CompletionNote, x.CompletedAtUtc, x.ReviewNote, x.Version))
            .ToListAsync(cancellationToken);
        var quizzes = await GetPublishedQuizzesAsync(assignment.PackageVersionIds, cancellationToken);
        var quiz = quizzes.FirstOrDefault();
        var readingItems = new List<OnboardingReadingItemDto>();
        var attempts = new List<OnboardingQuizAttemptSummaryDto>();
        if (assignment.CurrentReadingCycleId is { } cycleId)
        {
            var reading = await (from item in db.OnboardingReadingProgresses.AsNoTracking()
                                 join step in db.OnboardingStepProgresses.AsNoTracking() on item.StepProgressId equals step.Id
                                 where item.ReadingCycleId == cycleId
                                 orderby step.SectionOrder, step.StepOrder
                                 select new OnboardingReadingItemDto(item.Id, item.StepProgressId, step.SectionTitle, step.Title,
                                     item.IsRequired, item.DocumentId, item.KnowledgeArticleId, item.Status, item.StartedAtUtc,
                                     item.ReadAtUtc, item.Version, item.MinimumReadingMinutes,
                                     item.StartedAtUtc != null && item.MinimumReadingMinutes != null
                                         ? item.StartedAtUtc.Value.AddMinutes(item.MinimumReadingMinutes.Value) : null))
                                 .ToListAsync(cancellationToken);
        }
        attempts.AddRange(await (from attempt in db.OnboardingQuizAttempts.AsNoTracking()
                                 join cycle in db.OnboardingReadingCycles.AsNoTracking() on attempt.ReadingCycleId equals cycle.Id
                                 where attempt.AssignmentId == assignment.Id
                                 orderby attempt.AttemptNumber descending
                                 select new OnboardingQuizAttemptSummaryDto(attempt.Id, attempt.AttemptNumber, cycle.CycleNumber,
                                     attempt.Status, attempt.ScorePercentage, attempt.PassingPercentage, attempt.StartedAtUtc,
                                     attempt.SubmittedAtUtc)).ToListAsync(cancellationToken));
        var required = steps.Where(x => x.IsRequired).ToArray();
        var readingByStep = readingItems.ToDictionary(x => x.StepProgressId);
        var completedRequired = required.Count(x => readingByStep.TryGetValue(x.Id, out var reading)
            ? reading.Status == OnboardingReadingStatus.Read
            : x.Status == OnboardingStepStatus.Completed);
        var progress = required.Length == 0 ? 100 : (int)Math.Round(completedRequired * 100m / required.Length);
        var requiredReading = readingItems.Where(x => x.IsRequired).ToArray();
        var readCount = requiredReading.Count(x => x.Status == OnboardingReadingStatus.Read);
        var readingPercent = requiredReading.Length == 0 ? 100 : (int)Math.Round(readCount * 100m / requiredReading.Length);
        var assessment = new OnboardingAssessmentDto(assignment.AssessmentStatus, assignment.CurrentReadingCycleNumber,
            requiredReading.Length, readCount, readingPercent, quiz?.Id, quiz?.Title,
            quiz is null ? null : CombinedAssessment.PassingPercentage(quizzes), readingItems, attempts);
        return new OnboardingAssignmentDto(assignment.Id, assignment.EmployeeId, employees.GetValueOrDefault(assignment.EmployeeId) ?? "Unknown",
            assignment.TemplateId, templateName, assignment.TemplateVersionNumber, assignment.ProjectId, projectName,
            assignment.ProjectTemplateId, projectTemplateName,
            assignment.ManagerEmployeeId, assignment.ManagerEmployeeId is { } manager ? employees.GetValueOrDefault(manager) : null,
            assignment.MentorEmployeeId, assignment.MentorEmployeeId is { } mentor ? employees.GetValueOrDefault(mentor) : null,
            assignment.StartDate, assignment.Status, progress, assignment.CompletedAtUtc, steps, assessment, assignment.Version);
    }

    private async Task<IReadOnlyList<ProjectDto>> VisibleProjectsForEmployeeAsync(Guid employeeId, DateOnly today, CancellationToken cancellationToken)
    {
        var projectIds = await db.ProjectMemberships.Where(x => x.OrganizationId == OrganizationId && x.EmployeeId == employeeId &&
                x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today)).Select(x => x.ProjectId).ToListAsync(cancellationToken);
        var projects = await db.Projects.AsNoTracking().Where(x => x.OrganizationId == OrganizationId && (projectIds.Contains(x.Id) || x.LeadEmployeeId == employeeId))
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var result = new List<ProjectDto>();
        foreach (var project in projects)
        {
            var leadName = await db.Employees.Where(x => x.Id == project.LeadEmployeeId).Select(x => x.FirstName + " " + x.LastName).SingleAsync(cancellationToken);
            var contactName = project.OnboardingContactEmployeeId is { } contactId
                ? await db.Employees.Where(x => x.Id == contactId).Select(x => x.FirstName + " " + x.LastName).SingleOrDefaultAsync(cancellationToken) : null;
            var departmentName = project.DepartmentId is { } departmentId
                ? await db.Departments.Where(x => x.Id == departmentId).Select(x => x.Name).SingleOrDefaultAsync(cancellationToken) : null;
            var members = await (from membership in db.ProjectMemberships.AsNoTracking()
                                 join member in db.Employees.AsNoTracking() on membership.EmployeeId equals member.Id
                                 where membership.ProjectId == project.Id && membership.EffectiveFrom <= today && (membership.EffectiveTo == null || membership.EffectiveTo >= today)
                                 orderby member.FirstName, member.LastName
                                 select new ProjectMemberDto(membership.Id, member.Id, member.FirstName + " " + member.LastName, membership.ProjectRole,
                                     membership.Responsibilities, membership.EffectiveFrom, membership.EffectiveTo, membership.Version)).ToListAsync(cancellationToken);
            var links = await db.ProjectResourceLinks.AsNoTracking().Where(x => x.ProjectId == project.Id).OrderBy(x => x.SortOrder)
                .Select(x => new ProjectResourceLinkDto(x.Id, x.Label, x.Url, x.ResourceType, x.SortOrder)).ToListAsync(cancellationToken);
            result.Add(new ProjectDto(project.Id, project.Code, project.Name, project.Purpose, project.Description, project.DepartmentId, departmentName,
                project.LeadEmployeeId, leadName, project.OnboardingContactEmployeeId, contactName, project.Status,
                project.LeadEmployeeId == employeeId, members, links, project.Version));
        }
        return result;
    }

}
