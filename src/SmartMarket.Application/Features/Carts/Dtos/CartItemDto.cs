using SmartMarket.Application.Common.Mappings;
using SmartMarket.Domain.Entities;

public class CartItemDto : IMapFrom<CartItem>
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}