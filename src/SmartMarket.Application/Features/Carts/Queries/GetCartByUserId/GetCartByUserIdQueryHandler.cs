using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Carts.Dtos;

namespace SmartMarket.Application.Features.Carts.Queries.GetCartByUserId;

public class GetCartByUserIdQueryHandler : IRequestHandler<GetCartByUserIdQuery, Result<CartDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCartByUserIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CartDto>> Handle(GetCartByUserIdQuery request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId)
            .ProjectTo<CartDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (cart == null)
        {
            return Result<CartDto>.Success(new CartDto
            {
                Id = Guid.Empty,
                UserId = request.UserId,
                Items = new List<CartItemDto>()
            });
        }

        return Result<CartDto>.Success(cart);
    }
}