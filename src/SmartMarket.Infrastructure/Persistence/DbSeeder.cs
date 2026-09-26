using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Domain.Entities;
using SmartMarket.Infrastructure.Persistence;

namespace SmartMarket.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        var adminEmail = configuration["AdminCredentials:Email"] ?? "admin@smartmarket.com";
        var adminPassword = configuration["AdminCredentials:Password"];

        if (string.IsNullOrEmpty(adminPassword))
        {
            return;
        }

        var adminExists = await context.Users.AnyAsync(u => u.Email == adminEmail);

        if (!adminExists)
        {
            var adminUser = new User("Admin", adminEmail, adminPassword, Domain.Enums.UserRole.SuperAdmin);

            adminUser.ChangePassword(passwordHasher.HashPassword(adminPassword));

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
    }
}