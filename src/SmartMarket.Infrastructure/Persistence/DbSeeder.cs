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
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // 1. Seed Admin User
        var adminEmail = configuration["AdminCredentials:Email"] ?? "admin@smartmarket.com";
        var adminPassword = configuration["AdminCredentials:Password"];

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (adminUser == null)
        {
            adminUser = new User("Admin", adminEmail, adminPassword!, UserRole.SuperAdmin);
            adminUser.ChangePassword(passwordHasher.HashPassword(adminPassword!));

            await context.Users.AddAsync(adminUser);
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
                new Store("TechZone", adminUser.Id, "Main Tech Store"),
                new Store("FreshMarket", adminUser.Id, "Supermarket & Groceries"),
                new Store("FashionHub", adminUser.Id, "Clothing & Accessories")
            };

            await context.Stores.AddRangeAsync(stores);
            await context.SaveChangesAsync();
        }

        // 4. Seed Products
        if (!await context.Products.AnyAsync())
        {
            var electronicsCategory = await context.Categories.FirstAsync(c => c.Name == "Electronics");
            var groceriesCategory = await context.Categories.FirstAsync(c => c.Name == "Groceries");
            var techStore = await context.Stores.FirstAsync(s => s.Name == "TechZone");
            var freshStore = await context.Stores.FirstAsync(s => s.Name == "FreshMarket");

            var products = new List<Product>
            {
                new Product("iPhone 15 Pro", "128GB Titanium", 999.99m, 10, electronicsCategory.Id, techStore.Id),
                new Product("Wireless Headphones", "Noise cancelling Bluetooth headphones", 149.50m, 25, electronicsCategory.Id, techStore.Id),
                new Product("Organic Milk 1L", "Fresh whole milk", 2.50m, 100, groceriesCategory.Id, freshStore.Id),
                new Product("Arabica Coffee Beans 500g", "Premium roasted coffee", 12.00m, 50, groceriesCategory.Id, freshStore.Id)
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