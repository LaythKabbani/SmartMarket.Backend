using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;

namespace SmartMarket.Application.Features.Stores.Commands.DeleteStore;

public class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteStoreCommandHandler> _logger;

    public DeleteStoreCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILogger<DeleteStoreCommandHandler> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.User;

        if (currentUser == null)
        {
            _logger.LogWarning("Unauthorized attempt to delete store with Id {StoreId}.", request.Id);
            return Result<bool>.Failure("Unauthorized access.");
        }

        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (store == null)
        {
            _logger.LogWarning("Delete store failed. StoreId {StoreId} not found.", request.Id);
            return Result<bool>.Failure("Store not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            currentUser,
            store,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Forbidden attempt to delete StoreId {StoreId} by UserId {UserId}.",
                request.Id, _currentUserService.UserId);
            return Result<bool>.Failure("You are not authorized to delete this store.");
        }

        store.MarkAsDeleted();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Store {StoreId} ({StoreName}) was soft-deleted successfully by UserId {UserId}.",
            store.Id, store.Name, _currentUserService.UserId);

        return Result<bool>.Success(true);
    }
}