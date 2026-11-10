using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Documents.Commands.CreateDocumentFolder;
using HrManagement.Application.Features.Documents.Queries.GetDocumentFolders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/document-folders"), Authorize(Policy = Policies.DocumentsRead)]
public sealed class DocumentFoldersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetDocumentFoldersQuery(), cancellationToken));

    [HttpPost, Authorize(Policy = Policies.DocumentsWrite)]
    public async Task<IActionResult> Create(CreateDocumentFolderCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedWithEtag($"/api/v1/document-folders/{result.Id}", result, result.Version);
    }
}
