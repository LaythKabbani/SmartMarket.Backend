using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Wishlist.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId) : IRequest<Result<bool>>;
