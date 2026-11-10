using HrManagement.Api.Configuration;
using HrManagement.Application.Common.Models;
using HrManagement.Application.Features.Documents.Commands.ArchiveDocument;
using HrManagement.Application.Features.Documents.Commands.UploadDocument;
using HrManagement.Application.Features.Documents.Commands.UploadDocumentVersion;
using HrManagement.Application.Features.Documents.Queries.DownloadDocument;
using HrManagement.Application.Features.Documents.Queries.GetEmployeeDocuments;
using HrManagement.Application.Features.Ai.Commands.ApproveDocumentForAi;
using HrManagement.Application.Features.Ai.Commands.ReprocessDocumentIndex;
using HrManagement.Application.Features.Ai.Commands.RevokeDocumentAiApproval;
using HrManagement.Application.Features.Ai.Queries.GetDocumentAiStatus;
using HrManagement.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/employees/{employeeId:guid}/documents"), Authorize(Policy = Policies.DocumentsRead)]
public sealed class DocumentsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid employeeId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetEmployeeDocumentsQuery(employeeId), cancellationToken));

    [HttpPost, Authorize(Policy = Policies.DocumentsUpload)]
    public async Task<IActionResult> Upload(Guid employeeId, [FromForm] IFormFile file, [FromForm] string documentType, [FromForm] Guid? folderId, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var upload = new DocumentUpload(file.FileName, file.ContentType, file.Length, stream, documentType);
        var result = await sender.Send(new UploadDocumentCommand(employeeId, upload, folderId), cancellationToken);
        return CreatedWithEtag($"/api/v1/employees/{employeeId}/documents/{result.Id}", result, result.Version);
    }

    [HttpPost("{documentId:guid}/versions"), Authorize(Policy = Policies.DocumentsWrite)]
    public async Task<IActionResult> UploadVersion(Guid employeeId, Guid documentId, [FromForm] IFormFile file, [FromForm] string documentType, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var upload = new DocumentUpload(file.FileName, file.ContentType, file.Length, stream, documentType);
        var result = await sender.Send(new UploadDocumentVersionCommand(employeeId, documentId, upload, ExpectedVersion()), cancellationToken);
        return CreatedWithEtag($"/api/v1/employees/{employeeId}/documents/{documentId}", result, result.Version);
    }

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid employeeId, Guid documentId, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new DownloadDocumentQuery(employeeId, documentId), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("{documentId:guid}"), Authorize(Policy = Policies.DocumentsWrite)]
    public async Task<IActionResult> Archive(Guid employeeId, Guid documentId, CancellationToken cancellationToken)
    {
        await sender.Send(new ArchiveDocumentCommand(employeeId, documentId, ExpectedVersion()), cancellationToken);
        return NoContent();
    }

    [HttpGet("{documentId:guid}/ai-status")]
    public async Task<IActionResult> AiStatus(Guid employeeId, Guid documentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetDocumentAiStatusQuery(employeeId, documentId), cancellationToken));

    [HttpPost("{documentId:guid}/ai-approval"), Authorize(Policy = Policies.DocumentsWrite)]
    public async Task<IActionResult> ApproveAi(Guid employeeId, Guid documentId, [FromBody] AiApprovalRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ApproveDocumentForAiCommand(employeeId, documentId, request.Classification), cancellationToken));

    [HttpDelete("{documentId:guid}/ai-approval"), Authorize(Policy = Policies.DocumentsWrite)]
    public async Task<IActionResult> RevokeAi(Guid employeeId, Guid documentId, CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeDocumentAiApprovalCommand(employeeId, documentId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{documentId:guid}/ai-reprocess"), Authorize(Policy = Policies.DocumentsWrite)]
    public async Task<IActionResult> ReprocessAi(Guid employeeId, Guid documentId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReprocessDocumentIndexCommand(employeeId, documentId), cancellationToken));

    public sealed record AiApprovalRequest(DataClassification Classification);
}
