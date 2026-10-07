using MediatR;

namespace SmartMarket.Application.Features.Products.Queries.GetProductsCount;

public record GetProductsCountQuery : IRequest<int>;
