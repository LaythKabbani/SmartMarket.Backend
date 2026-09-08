using SmartMarket.Application.Common.Mappings;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Carts.Dtos;

public class CartDto : IMapFrom<Cart>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalPrice => Items.Sum(i => i.UnitPrice * i.Quantity);
}