using SmartMarket.Domain.Enums;

namespace SmartMarket.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string ShippingAddress { get; private set; } = default!;

    // Navigation Properties
    public User User { get; private set; } = default!;
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();

    private Order() { }

    public Order(Guid userId, decimal totalAmount, string shippingAddress)
    {
        UserId = userId;
        TotalAmount = totalAmount;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    // Navigation Properties
    public Order Order { get; private set; } = default!;
    public Product Product { get; private set; } = default!;

    private OrderItem() { }

    public OrderItem(Guid orderId, Guid productId, int quantity, decimal unitPrice)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}