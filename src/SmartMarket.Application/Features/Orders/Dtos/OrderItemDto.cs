using SmartMarket.Application.Common.Mappings;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Orders.Dtos;

public class OrderItemDto : IMapFrom<OrderItem>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}