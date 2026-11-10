using HrManagement.Application.Common.Exceptions;
using HrManagement.Application.Common.Models;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class ApplicationContractTests
{
    [Fact]
    public void Validation_exception_preserves_field_errors()
    {
        var errors = new Dictionary<string, string[]> { ["email"] = ["Email is invalid."] };
        var exception = new ValidationException(errors);
        Assert.Same(errors, exception.Errors);
    }

    [Fact]
    public void Page_result_preserves_cursor()
    {
        var page = new PageResult<int>([1, 2], "next");
        Assert.Equal("next", page.NextCursor);
        Assert.Equal([1, 2], page.Items);
    }
}
