using HrManagement.Application.Features.Employees.Commands.CreateEmployee;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class CreateEmployeeCommandValidatorTests
{
    [Fact]
    public async Task Rejects_invalid_required_fields_and_email()
    {
        var validator = new CreateEmployeeCommandValidator();
        var result = await validator.ValidateAsync(new CreateEmployeeCommand(string.Empty, string.Empty, string.Empty, "not-an-email", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateEmployeeCommand.EmployeeNumber));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateEmployeeCommand.WorkEmail));
    }
}
