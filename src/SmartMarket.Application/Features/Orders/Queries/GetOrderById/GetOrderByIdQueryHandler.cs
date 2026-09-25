using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Application.Features.Orders.Dtos;

namespace SmartMarket.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderByIdQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            return Result<OrderDto>.Failure("Unauthorized access.");
        }

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Store)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
        {
            return Result<OrderDto>.Failure("Order not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            order,
            new SameAuthorOrStoreOwnerOrAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            return Result<OrderDto>.Failure("Order not found or access denied.");
        }

        var orderDto = _mapper.Map<OrderDto>(order);

        return Result<OrderDto>.Success(orderDto);
    }
}