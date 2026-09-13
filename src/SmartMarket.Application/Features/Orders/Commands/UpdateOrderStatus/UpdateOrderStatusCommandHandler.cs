using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Orders.Commands.UpdateOrderStatus;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateOrderStatusCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            return Result<bool>.Failure("Unauthorized access.");
        }

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Store)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            return Result<bool>.Failure("Order not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            order,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            return Result<bool>.Failure("You are not authorized to update this order's status.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return Result<bool>.Failure("Cannot change status of a cancelled order.");
        }

        if (request.Status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    product.UpdateStock(product.StockQuantity + item.Quantity);
                }
            }
        }

        order.UpdateStatus(request.Status);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}