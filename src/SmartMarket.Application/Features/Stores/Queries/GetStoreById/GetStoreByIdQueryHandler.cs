using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Stores.Dtos;

namespace SmartMarket.Application.Features.Stores.Queries.GetStoreById;

public class GetStoreByIdQueryHandler : IRequestHandler<GetStoreByIdQuery, Result<StoreDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStoreByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<StoreDto>> Handle(GetStoreByIdQuery request, CancellationToken cancellationToken)
    {
        var store = await _context.Stores
            .AsNoTracking()
            .Where(s => s.Id == request.Id)
            .ProjectTo<StoreDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (store == null)
        {
            return Result<StoreDto>.Failure("Store not found.");
        }

        return Result<StoreDto>.Success(store);
    }
}