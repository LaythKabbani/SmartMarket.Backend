using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;

namespace SmartMarket.Application.Features.Stores.Commands.DeleteStore;

public class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteStoreCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.User;

        if (currentUser == null)
        {
            return Result<bool>.Failure("Unauthorized access.");
        }

        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (store == null)
        {
            return Result<bool>.Failure("Store not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            currentUser,
            store,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            return Result<bool>.Failure("You are not authorized to delete this store.");
        }

        store.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}