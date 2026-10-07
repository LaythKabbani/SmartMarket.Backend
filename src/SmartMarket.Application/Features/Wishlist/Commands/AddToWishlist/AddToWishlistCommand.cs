using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Wishlist.Commands.AddToWishlist;

public record AddToWishlistCommand(Guid UserId, Guid ProductId) : IRequest<Result<bool>>;
