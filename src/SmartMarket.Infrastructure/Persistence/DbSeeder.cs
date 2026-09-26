using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using SmartMarket.Infrastructure.Persistence;

namespace SmartMarket.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // 1. Seed Main Admin User
        var adminEmail = configuration["AdminCredentials:Email"] ?? "admin@smartmarket.com";
        var adminPassword = configuration["AdminCredentials:Password"] ?? "Password123!";

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (adminUser == null)
        {
            var hashedPassword = passwordHasher.HashPassword(adminPassword);
            adminUser = new User("Admin", adminEmail, hashedPassword, UserRole.SuperAdmin);
            adminUser.ChangePassword(hashedPassword);

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }

        // Seeding some Merchants
        var techOwnerEmail = "tech_owner@smartmarket.com";
        var freshOwnerEmail = "fresh_owner@smartmarket.com";

        var techOwner = await context.Users.FirstOrDefaultAsync(u => u.Email == techOwnerEmail);
        if (techOwner == null)
        {
            var hashedPassword = passwordHasher.HashPassword("Password123!");
            techOwner = new User("Tech Owner", techOwnerEmail, hashedPassword, UserRole.Merchant);
            await context.Users.AddAsync(techOwner);
            await context.SaveChangesAsync();
        }

        var freshOwner = await context.Users.FirstOrDefaultAsync(u => u.Email == freshOwnerEmail);
        if (freshOwner == null)
        {
            var hashedPassword = passwordHasher.HashPassword("Password123!");
            freshOwner = new User("Fresh Owner", freshOwnerEmail, hashedPassword, UserRole.Merchant);
            await context.Users.AddAsync(freshOwner);
            await context.SaveChangesAsync();
        }

        // 2. Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category("Electronics", "Electronic devices, phones and accessories"),
                new Category("Groceries", "Fresh food, drinks, and daily essentials"),
                new Category("Fashion", "Clothing, shoes, and lifestyle items"),
                new Category("Home & Kitchen", "Appliances, furniture, and home decor"),
                new Category("Books & Stationery", "Books, notebooks, and office supplies"),
                new Category("Sports & Outdoors", "Sports equipment and outdoor gear"),
                new Category("Health & Beauty", "Personal care, cosmetics, and wellness products"),
                new Category("Toys & Games", "Toys, games, and entertainment for kids"),
                new Category("Automotive", "Car accessories, tools, and automotive products"),
                new Category("Pet Supplies", "Food, toys, and accessories for pets")
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 3. Seed Stores
        if (!await context.Stores.AnyAsync())
        {
            var stores = new List<Store>
            {
                new Store("TechZone", techOwner.Id, "Main Tech Store", "https://images.unsplash.com/photo-1526738549149-8e07eca6c147?w=800"),
                new Store("FreshMarket", freshOwner.Id, "Supermarket & Groceries"),
                new Store("FashionHub", adminUser.Id, "Clothing & Accessories", "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=800")
            };

            await context.Stores.AddRangeAsync(stores);
            await context.SaveChangesAsync();
        }

        // 4. Seed Products
        if (!await context.Products.AnyAsync())
        {
            var electronicsCategory = await context.Categories.AsNoTracking().FirstAsync(c => c.Name == "Electronics");
            var groceriesCategory = await context.Categories.AsNoTracking().FirstAsync(c => c.Name == "Groceries");
            var techStore = await context.Stores.AsNoTracking().FirstAsync(s => s.Name == "TechZone");
            var freshStore = await context.Stores.AsNoTracking().FirstAsync(s => s.Name == "FreshMarket");

            var products = new List<Product>
            {
                new Product("iPhone 15 Pro", "128GB Titanium", 999.99m, 10, techStore.Id, electronicsCategory.Id,
                imageUrl: "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=500"),
                new Product("Wireless Headphones", "Noise cancelling Bluetooth headphones", 149.50m, 25, techStore.Id, electronicsCategory.Id,
                imageUrl: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500"),
                new Product("Arabica Coffee Beans 500g", "Premium roasted coffee", 12.00m, 50, freshStore.Id, groceriesCategory.Id,
                imageUrl: "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=500"),
                new Product("Organic Milk 1L", "Fresh whole milk", 2.50m, 100, freshStore.Id, groceriesCategory.Id),
                new Product("USB-C Fast Charger 20W", "Compact power adapter", 19.99m, 40, techStore.Id, electronicsCategory.Id)
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        // 5. Seed Orders
        if (!await context.Orders.AnyAsync())
        {
            var sampleProduct = await context.Products.FirstOrDefaultAsync();

            if (sampleProduct != null)
            {
                var order = new Order(adminUser.Id, 50M, "Damascus - Syria");
                context.Orders.Add(order);
                await context.SaveChangesAsync();

                order.Items.Add(new OrderItem(order.Id, sampleProduct.Id, quantity: 2, unitPrice: sampleProduct.Price));

                await context.SaveChangesAsync();
            }
        }
    }
}