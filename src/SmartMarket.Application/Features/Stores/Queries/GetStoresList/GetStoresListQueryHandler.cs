using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Mappings;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Stores.Dtos;

namespace SmartMarket.Application.Features.Stores.Queries.GetStoresList;

public class GetStoresListQueryHandler : IRequestHandler<GetStoresListQuery, PaginatedList<StoreDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStoresListQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<StoreDto>> Handle(GetStoresListQuery request, CancellationToken cancellationToken)
    {
        return await _context.Stores
            .AsNoTracking()
            .ProjectTo<StoreDto>(_mapper.ConfigurationProvider)
            .PaginateAsync(request.PageNumber, request.PageSize);
    }
}