using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Wishlist.Commands.AddToWishlist;

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public AddToWishlistCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(AddToWishlistCommand request, CancellationToken ct)
    {
        if (_currentUserService.User == null)
            return Result<bool>.Failure("Unauthorized access.");

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            new WishlistItem(request.UserId, request.ProductId),
            new OwnerOrSuperAdminRequirement());

        if (!authResult.Succeeded)
            return Result<bool>.Failure("Not authorized.");

        var exists = await _context.WishlistItems
            .AnyAsync(w => w.UserId == request.UserId && w.ProductId == request.ProductId, ct);
        if (exists) return Result<bool>.Failure("Already in wishlist.");

        var item = new WishlistItem(request.UserId, request.ProductId);
        _context.WishlistItems.Add(item);
        await _context.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
