using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            _logger.LogWarning("Unauthorized attempt to cancel order {OrderId}", request.OrderId);
            return Result<bool>.Failure("Unauthorized access.");
        }

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Store)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Cancel order failed. OrderId {OrderId} not found.", request.OrderId);
            return Result<bool>.Failure("Order not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            order,
            new SameAuthorOrStoreOwnerOrAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Forbidden attempt to cancel order {OrderId} by UserId {UserId}",
                request.OrderId, _currentUserService.UserId);
            return Result<bool>.Failure("You are not authorized to cancel this order.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning("Failed to cancel OrderId {OrderId}. Reason: Order status is {Status}, not Pending.",
                order.Id, order.Status);
            return Result<bool>.Failure("Only pending orders can be cancelled.");
        }

        order.UpdateStatus(OrderStatus.Cancelled);

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

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} cancelled successfully by UserId {UserId}. Stock restored.",
            order.Id, _currentUserService.UserId);

        return Result<bool>.Success(true);
    }
}