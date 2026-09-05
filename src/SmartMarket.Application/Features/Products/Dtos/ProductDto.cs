using AutoMapper;
using SmartMarket.Application.Common.Mappings;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Products.Dtos;

public class ProductDto : IMapFrom<Product>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = default!;

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Product, ProductDto>()
            .ForMember(d => d.StoreName, opt => opt.MapFrom(s => s.Store.Name))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name));
    }
}