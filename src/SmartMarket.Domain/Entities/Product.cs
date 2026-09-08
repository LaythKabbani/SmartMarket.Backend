namespace SmartMarket.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public string? ImageUrl { get; private set; }

    public Guid StoreId { get; private set; }
    public Guid CategoryId { get; private set; }

    // Navigation Properties
    public Store Store { get; private set; } = default!;
    public Category Category { get; private set; } = default!;

    private Product() { }

    public Product(string name, string description, decimal price, int stockQuantity, Guid storeId, Guid categoryId, string? imageUrl = null)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        StoreId = storeId;
        CategoryId = categoryId;
        ImageUrl = imageUrl;
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new InvalidOperationException($"Cannot reduce stock below zero. Current stock: {StockQuantity}, requested change: {quantity}");
        }
        
        StockQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string description, decimal price, int stockQuantity, Guid categoryId)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
    }
}