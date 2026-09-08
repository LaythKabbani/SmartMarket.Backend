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
        var productIds = request.Items.Select(i => i.ProductId).ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Distinct().Count())
        {
            return Result<Guid>.Failure("One or more products were not found.");
        }

        decimal totalAmount = 0;
        foreach (var itemRequest in request.Items)
        {
            var product = products.First(p => p.Id == itemRequest.ProductId);

            if (product.StockQuantity < itemRequest.Quantity)
            {
                return Result<Guid>.Failure($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}");
            }

            totalAmount += product.Price * itemRequest.Quantity;
        }

        var order = new Order(request.UserId, totalAmount, request.ShippingAddress);

        foreach (var itemRequest in request.Items)
        {
            var product = products.First(p => p.Id == itemRequest.ProductId);

            product.UpdateStock(product.StockQuantity - itemRequest.Quantity);

            var orderItem = new OrderItem(order.Id, product.Id, itemRequest.Quantity, product.Price);
            order.Items.Add(orderItem);
        }

        _context.Orders.Add(order);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(order.Id);
    }
}