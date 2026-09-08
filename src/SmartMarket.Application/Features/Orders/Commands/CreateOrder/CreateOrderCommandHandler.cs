using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null || !cart.Items.Any())
        {
            return Result<Guid>.Failure("Cart is empty. Add products to cart before checkout.");
        }

        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Distinct().Count())
        {
            return Result<Guid>.Failure("One or more products in the cart are no longer available.");
        }

        decimal totalAmount = 0;
        foreach (var cartItem in cart.Items)
        {
            var product = products.First(p => p.Id == cartItem.ProductId);

            if (product.StockQuantity < cartItem.Quantity)
            {
                return Result<Guid>.Failure($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}");
            }

            totalAmount += product.Price * cartItem.Quantity;
        }

        var order = new Order(request.UserId, totalAmount, request.ShippingAddress);

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

        return Result<Guid>.Success(order.Id);
    }
}