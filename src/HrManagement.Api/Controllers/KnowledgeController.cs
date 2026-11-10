using HrManagement.Application.Features.Knowledge.Commands.AddKnowledgeRevision;
using HrManagement.Application.Features.Knowledge.Commands.ArchiveKnowledgeArticle;
using HrManagement.Application.Features.Knowledge.Commands.CreateKnowledgeArticle;
using HrManagement.Application.Features.Knowledge.Commands.PublishKnowledgeArticle;
using HrManagement.Application.Features.Knowledge.Commands.SubmitKnowledgeForReview;
using HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeArticle;
using HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeArticles;
using HrManagement.Application.Features.Knowledge.Commands.UploadKnowledgeDocument;
using HrManagement.Application.Features.Knowledge.Queries.DownloadKnowledgeDocument;
using HrManagement.Application.Features.Knowledge.Queries.GetKnowledgeDocuments;
using HrManagement.Application.Features.Knowledge.Commands.ApproveKnowledgeDocumentForAi;
using HrManagement.Domain;
using HrManagement.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/knowledge/articles"), Authorize]
public sealed class KnowledgeController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? projectId, [FromQuery] string? category, [FromQuery] string? search,
        [FromQuery] bool includeDrafts = false, CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetKnowledgeArticlesQuery(projectId, category, search, includeDrafts), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetKnowledgeArticleQuery(id), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateKnowledgeArticleCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedWithEtag($"/api/v1/knowledge/articles/{result.Id}", result, result.Version);
    }

    [HttpPost("{id:guid}/revisions")]
    public async Task<IActionResult> AddRevision(Guid id, AddKnowledgeRevisionCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { ArticleId = id, ExpectedVersion = ExpectedVersion() }, cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> SubmitReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SubmitKnowledgeForReviewCommand(id, ExpectedVersion()), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishKnowledgeArticleCommand(id, ExpectedVersion()), cancellationToken);
        return WithEtag(result, result.Version);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ArchiveKnowledgeArticleCommand(id, ExpectedVersion()), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> Documents(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetKnowledgeDocumentsQuery(id), cancellationToken));

    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] IFormFile file, [FromForm] string documentType, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await sender.Send(new UploadKnowledgeDocumentCommand(id,
            new DocumentUpload(file.FileName, file.ContentType, file.Length, stream, documentType)), cancellationToken);
        return CreatedWithEtag($"/api/v1/knowledge/articles/{id}/documents/{result.Id}", result, result.Version);
    }

    [HttpGet("{id:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new DownloadKnowledgeDocumentQuery(id, documentId), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    [HttpPost("{id:guid}/documents/{documentId:guid}/ai-approval")]
    public async Task<IActionResult> ApproveDocumentAi(Guid id, Guid documentId, KnowledgeAiApprovalRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ApproveKnowledgeDocumentForAiCommand(id, documentId, request.Classification), cancellationToken));

    public sealed record KnowledgeAiApprovalRequest(DataClassification Classification);
}
