using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Application.Features.Wishlist.Commands.RemoveFromWishlist;

public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFromWishlistCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(RemoveFromWishlistCommand request, CancellationToken ct)
    {
        if (_currentUserService.User == null)
            return Result<bool>.Failure("Unauthorized access.");

        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == request.UserId && w.ProductId == request.ProductId, ct);
        if (item == null) return Result<bool>.Failure("Not found.");

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            item,
            new OwnerOrSuperAdminRequirement());

        if (!authResult.Succeeded)
            return Result<bool>.Failure("Not authorized.");

        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
