using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Stores.Commands.CreateStore;

public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public CreateStoreCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.User;

        if (currentUser == null || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            return Result<Guid>.Failure("Unauthorized access.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            currentUser,
            null,
            new CanCreateStoreRequirement()
        );

        if (!authResult.Succeeded)
        {
            return Result<Guid>.Failure("Only merchants and admins are authorized to create stores.");
        }

        var isSuperAdmin = currentUser.IsInRole(UserRole.SuperAdmin.ToString());
        var targetOwnerId = isSuperAdmin ? request.OwnerId : currentUserId;

        var hasExistingStore = await _context.Stores
            .AnyAsync(s => s.OwnerId == targetOwnerId, cancellationToken);

        if (hasExistingStore)
        {
            return Result<Guid>.Failure("You already own a registered store. Each merchant is limited to one store only.");
        }

        var store = new Store(
            request.Name,
            targetOwnerId,
            request.Description,
            request.LogoUrl
        );

        _context.Stores.Add(store);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(store.Id);
    }
}