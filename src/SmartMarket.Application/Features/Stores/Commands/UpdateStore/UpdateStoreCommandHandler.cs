using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;

namespace SmartMarket.Application.Features.Stores.Commands.UpdateStore;

public class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateStoreCommandHandler> _logger;

    public UpdateStoreCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILogger<UpdateStoreCommandHandler> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            _logger.LogWarning("Unauthorized attempt to update store with Id {StoreId}.", request.Id);
            return Result<bool>.Failure("Unauthorized access.");
        }

        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (store == null)
        {
            _logger.LogWarning("Update store failed. StoreId {StoreId} not found.", request.Id);
            return Result<bool>.Failure("Store not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            store,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Forbidden attempt to update StoreId {StoreId} by UserId {UserId}.",
                request.Id, _currentUserService.UserId);
            return Result<bool>.Failure("You are not authorized to update this store.");
        }

        var oldName = store.Name;
        store.Update(request.Name, request.Description, request.LogoUrl);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Store {StoreId} details updated successfully by UserId {UserId}. Old Name: {OldName}, New Name: {NewName}",
            store.Id, _currentUserService.UserId, oldName, request.Name);

        return Result<bool>.Success(true);
    }
}