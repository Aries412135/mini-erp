using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniErp.Application.Common.Interface;
using MiniErp.Domain.Constants;
using MiniErp.Domain.Entities;

namespace MiniErp.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Seed:AdminEmail / Seed:AdminPassword belum diset, seed admmin dilewati.");
            return;
        }

        email = email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var adminRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Admin);

        var user = User.Create(email, hasher.Hash(password), "Adminitrator");
        user.AssignRole(adminRole.Id);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        logger.LogInformation("Akun admin development dibuat: {Email}", email);
    }
}