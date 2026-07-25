using Microsoft.AspNetCore.Identity;
using TimeTracker.Models.Entities;

namespace TimeTracker.Data;

public static class ApplicationDbSeeder
{
    public static async Task SeedDevelopmentUserAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var username = configuration["SeedUser:Username"];
        var password = configuration["SeedUser:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Development seed-user credentials are missing.");
        }

        if (await userManager.FindByNameAsync(username) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = username
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Could not create the development seed user: {errors}");
        }
    }
}
