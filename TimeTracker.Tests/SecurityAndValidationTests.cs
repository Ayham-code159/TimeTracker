using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using TimeTracker.Controllers;
using TimeTracker.Models.Dtos.LegacyProjectDtos;
using TimeTracker.Models.Dtos.ProjectDtos;
using TimeTracker.Security;
using TimeTracker.Validators.LegacyProjectValidators;
using TimeTracker.Validators.ProjectValidators;
using Xunit;

namespace TimeTracker.Tests;

public class SecurityAndValidationTests
{
    [Theory]
    [InlineData(typeof(ProjectController))]
    [InlineData(typeof(LegacyProjectController))]
    public void ProtectedControllersRequireAdmin(Type controllerType)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(ApplicationRoles.Admin, authorize.Roles);
    }

    [Fact]
    public void AuthLifecycleEndpointsAreAnonymousOnAuthController()
    {
        var anonymousActions = typeof(AuthController).GetMethods()
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(method => method.Name)
            .ToList();

        Assert.Equal(
            ["Login", "Logout", "Refresh"],
            anonymousActions.OrderBy(name => name).ToList());
    }

    [Fact]
    public async Task ManualTimeRejectsZeroDuration()
    {
        var result = await new AddManualTimeDtoValidator().ValidateAsync(
            new AddManualTimeDto { Hours = 0, Minutes = 0 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task LegacyProjectRejectsInvalidColorAndWhitespaceName()
    {
        var result = await new CreateLegacyProjectDtoValidator().ValidateAsync(
            new CreateLegacyProjectDto
            {
                Name = " Project ",
                Color = "pink"
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Name");
        Assert.Contains(result.Errors, error => error.PropertyName == "Color");
    }
}
