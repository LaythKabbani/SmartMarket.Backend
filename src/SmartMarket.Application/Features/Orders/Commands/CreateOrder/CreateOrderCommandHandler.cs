using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            _logger.LogWarning("Unauthorized order creation attempt.");
            return Result<Guid>.Failure("Unauthorized access.");
        }

        if (request.UserId != currentUserId)
        {
            _logger.LogWarning("Forbidden order creation attempt. CurrentUser {CurrentUserId} tried to create order for TargetUser {TargetUserId}",
                currentUserId, request.UserId);
            return Result<Guid>.Failure("You can only create orders for your own account.");
        }

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == currentUserId, cancellationToken);

        if (cart == null || !cart.Items.Any())
        {
            _logger.LogWarning("Order creation failed for UserId: {UserId}. Reason: Cart is empty.", currentUserId);
            return Result<Guid>.Failure("Cart is empty. Add products to cart before checkout.");
        }

        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Distinct().Count())
        {
            _logger.LogWarning("Order creation failed for UserId: {UserId}. Reason: Some products are no longer available.", currentUserId);
            return Result<Guid>.Failure("One or more products in the cart are no longer available.");
        }

        decimal totalAmount = 0;
        foreach (var cartItem in cart.Items)
        {
            var product = products.First(p => p.Id == cartItem.ProductId);

            if (product.StockQuantity < cartItem.Quantity)
            {
                _logger.LogWarning("Order creation failed for UserId: {UserId}. Reason: Insufficient stock for ProductId {ProductId}. Requested: {RequestedQuantity}, Available: {StockQuantity}",
                    currentUserId, product.Id, cartItem.Quantity, product.StockQuantity);
                return Result<Guid>.Failure($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}");
            }

            totalAmount += product.Price * cartItem.Quantity;
        }

        var order = new Order(currentUserId, totalAmount, request.ShippingAddress);

        foreach (var cartItem in cart.Items)
        {
            var product = products.First(p => p.Id == cartItem.ProductId);

            product.UpdateStock(product.StockQuantity - cartItem.Quantity);

            var orderItem = new OrderItem(order.Id, product.Id, cartItem.Quantity, product.Price);
            order.Items.Add(orderItem);
        }

        _context.Orders.Add(order);

        cart.Clear();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order created successfully. OrderId: {OrderId}, UserId: {UserId}, TotalAmount: {TotalAmount}, ItemsCount: {ItemsCount}",
            order.Id, currentUserId, totalAmount, order.Items.Count);

        return Result<Guid>.Success(order.Id);
    }
}