using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Application.Features.Wishlist.Dtos;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Wishlist.Queries.GetWishlist;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, Result<List<WishlistItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public GetWishlistQueryHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<WishlistItemDto>>> Handle(GetWishlistQuery request, CancellationToken ct)
    {
        if (_currentUserService.User == null)
            return Result<List<WishlistItemDto>>.Failure("Unauthorized access.");

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            new WishlistItem(request.UserId, Guid.Empty),
            new OwnerOrSuperAdminRequirement());

        if (!authResult.Succeeded)
            return Result<List<WishlistItemDto>>.Failure("Not authorized.");

        var items = await _context.WishlistItems
            .Where(w => w.UserId == request.UserId)
            .Select(w => new WishlistItemDto(w.ProductId, w.Product.Name))
            .ToListAsync(ct);
        return Result<List<WishlistItemDto>>.Success(items);
    }
}
