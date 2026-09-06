namespace SmartMarket.Domain.Entities;

public class Store : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public Guid OwnerId { get; private set; }

    // Navigation Properties
    public User Owner { get; private set; } = default!;
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Store() { }

    public Store(string name, Guid ownerId, string? description = null, string? logoUrl = null)
    {
        Name = name;
        OwnerId = ownerId;
        Description = description;
        LogoUrl = logoUrl;
    }

    public void Update(string name, string? description, string? logoUrl)
    {
        Name = name;
        Description = description;
        LogoUrl = logoUrl;
    }
}