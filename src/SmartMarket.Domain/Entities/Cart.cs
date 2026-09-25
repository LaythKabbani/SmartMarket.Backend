namespace SmartMarket.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    private readonly List<CartItem> _items = new();

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private Cart() { }

    public Cart(Guid userId)
    {
        UserId = userId;
    }

    public void AddOrUpdateItem(Guid productId, int quantity)
    {
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var item = new CartItem(Id, productId, quantity);
            _items.Add(item);
        }
    }

    public void RemoveItem(Guid productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            _items.Remove(item);
        }
    }

    public void Clear()
    {
        _items.Clear();
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

    public void UpdateQuantity(int newQuantity)
    {
        Quantity = newQuantity;
    }
}