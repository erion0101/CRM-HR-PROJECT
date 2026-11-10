using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Onboarding.Commands.AssignOnboarding;
using HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingTemplate;
using HrManagement.Application.Features.Onboarding.Commands.PublishOnboardingTemplate;
using HrManagement.Application.Features.Onboarding.Commands.ReviewOnboardingStep;
using HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingStep;
using HrManagement.Application.Features.Onboarding.Commands.SetOnboardingAutoAssign;
using HrManagement.Application.Features.Onboarding.Commands.UpdateOnboardingTemplate;
using HrManagement.Application.Features.Onboarding.Commands.CancelOnboarding;
using HrManagement.Application.Features.Onboarding.Commands.CreateOnboardingQuiz;
using HrManagement.Application.Features.Onboarding.Commands.AddQuizQuestion;
using HrManagement.Application.Features.Onboarding.Commands.UpdateQuizQuestion;
using HrManagement.Application.Features.Onboarding.Commands.DeleteQuizQuestion;
using HrManagement.Application.Features.Onboarding.Commands.GenerateOnboardingQuiz;
using HrManagement.Application.Features.Onboarding.Commands.PublishOnboardingQuiz;
using HrManagement.Application.Features.Onboarding.Commands.UpdateReadingProgress;
using HrManagement.Application.Features.Onboarding.Commands.StartQuizAttempt;
using HrManagement.Application.Features.Onboarding.Commands.SubmitQuizAttempt;
using HrManagement.Application.Features.Onboarding.Queries.GetMyOnboarding;
using HrManagement.Application.Features.Onboarding.Queries.GetOnboardingAssignments;
using HrManagement.Application.Features.Onboarding.Queries.GetOnboardingInsights;
using HrManagement.Application.Features.Onboarding.Queries.GetOnboardingTemplates;
using HrManagement.Application.Features.Onboarding.Queries.GetOnboardingQuiz;
using HrManagement.Application.Features.Onboarding.Queries.GetQuizAttempt;
using HrManagement.Application.Features.Onboarding.Queries.DownloadOnboardingDocument;
using HrManagement.Application.Features.Onboarding.Queries.GetOnboardingQuizSources;
using HrManagement.Application.Features.Onboarding;
using HrManagement.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/onboarding"), Authorize]
public sealed class OnboardingController(ISender sender) : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) => Ok(await sender.Send(new GetMyOnboardingQuery(), cancellationToken));

    [HttpGet("templates")]
    public async Task<IActionResult> Templates([FromQuery] bool publishedOnly = true, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetOnboardingTemplatesQuery(publishedOnly), cancellationToken));

    [HttpPost("templates"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> CreateTemplate(CreateOnboardingTemplateCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedWithEtag($"/api/v1/onboarding/templates/{result.Id}", result, result.Version);
    }

    [HttpPost("templates/{id:guid}/publish"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> PublishTemplate(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishOnboardingTemplateCommand(id, ExpectedVersion()), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPut("templates/{id:guid}/auto-assign"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> SetTemplateAutoAssign(Guid id, [FromBody] AutoAssignRequest body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetOnboardingAutoAssignCommand(id, body.Enabled, ExpectedVersion()), cancellationToken);
        return WithEtag(result, result.Version);
    }

    public sealed record AutoAssignRequest(bool Enabled);

    [HttpPut("templates/{id:guid}"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> UpdateTemplate(Guid id, UpdateOnboardingTemplateCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { TemplateId = id, ExpectedVersion = ExpectedVersion() }, cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpGet("insights"), Authorize(Policy = Policies.OnboardingManage)]
    public async Task<IActionResult> Insights(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetOnboardingInsightsQuery(), cancellationToken));

    [HttpGet("assignments"), Authorize(Policy = Policies.OnboardingManage)]
    public async Task<IActionResult> Assignments([FromQuery] Guid? employeeId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetOnboardingAssignmentsQuery(employeeId), cancellationToken));

    [HttpPost("assignments"), Authorize(Policy = Policies.OnboardingManage)]
    public async Task<IActionResult> Assign(AssignOnboardingCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedWithEtag($"/api/v1/onboarding/assignments/{result.Id}", result, result.Version);
    }

    [HttpPost("assignments/{assignmentId:guid}/cancel"), Authorize(Policy = Policies.OnboardingManage)]
    public async Task<IActionResult> Cancel(Guid assignmentId, CancelAssignmentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOnboardingCommand(assignmentId, request.Reason, ExpectedVersion()), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPut("assignments/{assignmentId:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> UpdateStep(Guid assignmentId, Guid stepId, UpdateStepRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateOnboardingStepCommand(assignmentId, stepId, request.Status, request.CompletionNote, ExpectedVersion()), cancellationToken);
        return Ok(result);
    }

    [HttpPost("assignments/{assignmentId:guid}/steps/{stepId:guid}/review"), Authorize(Policy = Policies.OnboardingManage)]
    public async Task<IActionResult> ReviewStep(Guid assignmentId, Guid stepId, ReviewStepRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReviewOnboardingStepCommand(assignmentId, stepId, request.Approved, request.ReviewNote, ExpectedVersion()), cancellationToken);
        return Ok(result);
    }

    [HttpGet("templates/{templateId:guid}/quiz"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> GetQuiz(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOnboardingQuizQuery(templateId), cancellationToken);
        return result is null ? NoContent() : WithEtag(result, result.Version);
    }

    [HttpGet("templates/{templateId:guid}/quiz-sources"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> GetQuizSources(Guid templateId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetOnboardingQuizSourcesQuery(templateId), cancellationToken));

    [HttpPost("templates/{templateId:guid}/quiz"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> CreateQuiz(Guid templateId, CreateQuizRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateOnboardingQuizCommand(templateId, request.Title, request.PassingPercentage,
            request.QuestionCount, request.RandomizeQuestions, request.RandomizeOptions), cancellationToken);
        return CreatedWithEtag($"/api/v1/onboarding/templates/{templateId}/quiz", result, result.Version);
    }

    [HttpPost("quizzes/{quizId:guid}/generate"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> GenerateQuiz(Guid quizId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GenerateOnboardingQuizCommand(quizId), cancellationToken));

    [HttpPost("quizzes/{quizId:guid}/questions"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> AddQuestion(Guid quizId, QuizQuestionInput question, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AddQuizQuestionCommand(quizId, question), cancellationToken));

    [HttpPut("quizzes/{quizId:guid}/questions/{questionId:guid}"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> UpdateQuestion(Guid quizId, Guid questionId, QuizQuestionInput question, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateQuizQuestionCommand(quizId, questionId, question, ExpectedVersion()), cancellationToken));

    [HttpDelete("quizzes/{quizId:guid}/questions/{questionId:guid}"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> DeleteQuestion(Guid quizId, Guid questionId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DeleteQuizQuestionCommand(quizId, questionId, ExpectedVersion()), cancellationToken));

    [HttpPost("quizzes/{quizId:guid}/publish"), Authorize(Policy = Policies.OnboardingAdmin)]
    public async Task<IActionResult> PublishQuiz(Guid quizId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishOnboardingQuizCommand(quizId, ExpectedVersion()), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPut("assignments/{assignmentId:guid}/reading/{stepId:guid}")]
    public async Task<IActionResult> UpdateReading(Guid assignmentId, Guid stepId, UpdateReadingRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateReadingProgressCommand(assignmentId, stepId, request.Status, ExpectedVersion()), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpGet("assignments/{assignmentId:guid}/reading/{stepId:guid}/download")]
    public async Task<IActionResult> DownloadReading(Guid assignmentId, Guid stepId, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new DownloadOnboardingDocumentQuery(assignmentId, stepId), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    [HttpPost("assignments/{assignmentId:guid}/quiz-attempts")]
    public async Task<IActionResult> StartAttempt(Guid assignmentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartQuizAttemptCommand(assignmentId), cancellationToken);
        return CreatedWithEtag($"/api/v1/onboarding/quiz-attempts/{result.Id}", result, result.Version);
    }

    [HttpGet("quiz-attempts/{attemptId:guid}")]
    public async Task<IActionResult> GetAttempt(Guid attemptId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetQuizAttemptQuery(attemptId), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPost("quiz-attempts/{attemptId:guid}/submit")]
    public async Task<IActionResult> SubmitAttempt(Guid attemptId, SubmitAttemptRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SubmitQuizAttemptCommand(attemptId, request.Answers, ExpectedVersion()), cancellationToken));

    public sealed record UpdateStepRequest(OnboardingStepStatus Status, string? CompletionNote);
    public sealed record ReviewStepRequest(bool Approved, string? ReviewNote);
    public sealed record CancelAssignmentRequest(string Reason);
    public sealed record CreateQuizRequest(string Title, int PassingPercentage = 80, int QuestionCount = 10,
        bool RandomizeQuestions = true, bool RandomizeOptions = true);
    public sealed record UpdateReadingRequest(OnboardingReadingStatus Status);
    public sealed record SubmitAttemptRequest(IReadOnlyList<SubmitQuizAnswerInput> Answers);
}
