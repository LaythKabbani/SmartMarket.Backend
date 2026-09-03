namespace SmartMarket.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    // Navigation Properties
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { }

    public Category(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}