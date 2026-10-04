using MediatR;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Dtos;

namespace SmartMarket.Application.Features.Subscriptions.Queries.GetSubscriptionByStoreId;

public record GetSubscriptionByStoreIdQuery(Guid StoreId) : IRequest<Result<SubscriptionDto>>;