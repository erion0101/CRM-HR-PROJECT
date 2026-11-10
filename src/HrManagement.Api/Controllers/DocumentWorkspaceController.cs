using HrManagement.Api.Configuration;
using HrManagement.Application.Features.Documents.Queries.SearchDocuments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[Route("api/v1/documents"), Authorize(Policy = Policies.DocumentsRead)]
public sealed class DocumentWorkspaceController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] Guid? folderId, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new SearchDocumentsQuery(folderId, search), cancellationToken));
}
