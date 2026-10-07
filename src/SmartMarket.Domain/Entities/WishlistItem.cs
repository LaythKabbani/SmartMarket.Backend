namespace SmartMarket.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public User User { get; private set; } = default!;
    public Product Product { get; private set; } = default!;

    private WishlistItem() { }
    public WishlistItem(Guid userId, Guid productId)
    {
        UserId = userId;
        ProductId = productId;
    }
}
