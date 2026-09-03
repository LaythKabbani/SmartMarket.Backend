namespace SmartMarket.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;
    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    private Cart() { }

    public Cart(Guid userId)
    {
        UserId = userId;
    }
}

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    // Navigation Properties
    public Cart Cart { get; private set; } = default!;
    public Product Product { get; private set; } = default!;

    private CartItem() { }

    public CartItem(Guid cartId, Guid productId, int quantity)
    {
        CartId = cartId;
        ProductId = productId;
        Quantity = quantity;
    }
}