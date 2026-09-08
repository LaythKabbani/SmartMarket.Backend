using SmartMarket.Application.Common.Mappings;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Orders.Dtos;

public class OrderDto : IMapFrom<Order>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = default!;
    public string ShippingAddress { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}