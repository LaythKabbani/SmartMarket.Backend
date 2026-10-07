using MediatR;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Wishlist.Dtos;

namespace SmartMarket.Application.Features.Wishlist.Queries.GetWishlist;

public record GetWishlistQuery(Guid UserId) : IRequest<Result<List<WishlistItemDto>>>;
