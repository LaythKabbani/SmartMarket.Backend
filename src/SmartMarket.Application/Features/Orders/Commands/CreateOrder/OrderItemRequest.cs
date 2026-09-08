namespace SmartMarket.Application.Features.Orders.Commands.CreateOrder;

public record OrderItemRequest(Guid ProductId, int Quantity);