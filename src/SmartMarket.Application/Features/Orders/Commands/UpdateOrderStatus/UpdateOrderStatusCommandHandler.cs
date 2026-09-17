using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            _logger.LogWarning("Unauthorized attempt to update status for OrderId {OrderId}.", request.OrderId);
            return Result<bool>.Failure("Unauthorized access.");
        }

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Store)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Update order status failed. OrderId {OrderId} not found.", request.OrderId);
            return Result<bool>.Failure("Order not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            order,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Forbidden attempt to update OrderId {OrderId} status by UserId {UserId}.",
                request.OrderId, _currentUserService.UserId);
            return Result<bool>.Failure("You are not authorized to update this order's status.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogWarning("Attempted to update status of an already cancelled OrderId {OrderId}.", order.Id);
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

            _logger.LogInformation("Order {OrderId} is being cancelled by admin/owner. Stock restored.", order.Id);
        }

        var oldStatus = order.Status;
        order.UpdateStatus(request.Status);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} status updated successfully from {OldStatus} to {NewStatus} by UserId {UserId}.",
            order.Id, oldStatus, request.Status, _currentUserService.UserId);

        return Result<bool>.Success(true);
    }
}