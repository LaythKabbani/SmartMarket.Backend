using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Products.Dtos;

namespace SmartMarket.Application.Features.Products.Queries.GetProductsList;

public class GetProductsListQueryHandler : IRequestHandler<GetProductsListQuery, PaginatedList<ProductDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetProductsListQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<ProductDto>> Handle(GetProductsListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsNoTracking();

        if (request.StoreId.HasValue)
        {
            query = query.Where(p => p.StoreId == request.StoreId.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
            .PaginateAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}