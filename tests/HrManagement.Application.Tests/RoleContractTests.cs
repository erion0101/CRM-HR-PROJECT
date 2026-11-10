using HrManagement.Application.Common.Authorization;
using Xunit;

namespace HrManagement.Application.Tests;

public sealed class RoleContractTests
{
    [Fact]
    public void Application_exposes_exactly_four_business_roles()
    {
        Assert.Equal(["Employee", "Manager", "HR", "ProductManager"], Roles.All);
        Assert.Equal(Roles.All.Order(), Roles.Descriptions.Keys.Order());
        Assert.All(Roles.Descriptions.Values, description => Assert.False(string.IsNullOrWhiteSpace(description)));
    }
}
