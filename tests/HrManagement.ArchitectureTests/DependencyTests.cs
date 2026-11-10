using HrManagement.Application;
using HrManagement.Domain;
using HrManagement.Infrastructure;
using NetArchTest.Rules;
using Xunit;

namespace HrManagement.ArchitectureTests;

public sealed class DependencyTests
{
    [Fact]
    public void Domain_has_no_dependencies_on_outer_layers()
    {
        var result = Types.InAssembly(typeof(Entity).Assembly)
            .ShouldNot().HaveDependencyOnAny("HrManagement.Application", "HrManagement.Infrastructure", "HrManagement.Api")
            .GetResult();
        Assert.True(result.IsSuccessful, Names(result));
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(typeof(HrManagement.Application.DependencyInjection).Assembly)
            .ShouldNot().HaveDependencyOnAny("HrManagement.Infrastructure", "HrManagement.Api")
            .GetResult();
        Assert.True(result.IsSuccessful, Names(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        var result = Types.InAssembly(typeof(AppDbContext).Assembly)
            .ShouldNot().HaveDependencyOn("HrManagement.Api")
            .GetResult();
        Assert.True(result.IsSuccessful, Names(result));
    }

    [Fact]
    public void Api_controllers_do_not_depend_on_entity_framework()
    {
        var result = Types.InAssembly(typeof(global::Program).Assembly)
            .That().ResideInNamespace("HrManagement.Api.Controllers")
            .ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();
        Assert.True(result.IsSuccessful, Names(result));
    }

    private static string Names(TestResult result) => string.Join(", ", result.FailingTypeNames ?? []);
}
