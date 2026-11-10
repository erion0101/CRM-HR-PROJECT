using HrManagement.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HrManagement.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected long ExpectedVersion()
    {
        var raw = Request.Headers.IfMatch.ToString().Trim().Trim('"');
        if (!long.TryParse(raw, out var value) || value <= 0)
            throw new ValidationException(new Dictionary<string, string[]> { ["If-Match"] = ["A valid entity version is required in the If-Match header."] });
        return value;
    }

    protected IActionResult WithEtag<T>(T value, long version)
    {
        Response.Headers.ETag = $"\"{version}\"";
        return Ok(value);
    }

    protected IActionResult CreatedWithEtag<T>(string location, T value, long version)
    {
        Response.Headers.ETag = $"\"{version}\"";
        return Created(location, value);
    }
}
