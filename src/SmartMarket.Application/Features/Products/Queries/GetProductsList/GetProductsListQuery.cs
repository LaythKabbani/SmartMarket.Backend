using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Products.Dtos;

namespace SmartMarket.Application.Features.Products.Queries.GetProductsList;

public record GetProductsListQuery : IRequest<PaginatedList<ProductDto>>
{
    public Guid? StoreId { get; init; }
    public Guid? CategoryId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}