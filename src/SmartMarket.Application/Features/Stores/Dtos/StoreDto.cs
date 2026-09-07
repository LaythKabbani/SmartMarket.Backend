using SmartMarket.Application.Common.Mappings;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Stores.Dtos;

public class StoreDto : IMapFrom<Store>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public Guid OwnerId { get; set; }
}