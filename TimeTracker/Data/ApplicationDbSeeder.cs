using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Models.Entities;
using TimeTracker.Security;

namespace TimeTracker.Data;

public static class ApplicationDbSeeder
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();
        var database = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await database.Database.MigrateAsync();

        foreach (var roleName in ApplicationRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(roleName));

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the '{roleName}' role: {FormatErrors(roleResult)}");
            }
        }

        var username = configuration["BootstrapAdmin:Username"];
        var password = configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment() &&
                await userManager.GetUsersInRoleAsync(ApplicationRoles.Admin) is { Count: > 0 })
                return;

            throw new InvalidOperationException(
                "Admin bootstrap credentials are missing. Set BootstrapAdmin__Username and BootstrapAdmin__Password.");
        }

        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the development seed user: {FormatErrors(result)}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(
                user,
                ApplicationRoles.Admin);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign the development seed user to the Admin role: {FormatErrors(roleResult)}");
            }
        }
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));
}
