using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;

namespace SmartMarket.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            _logger.LogWarning("Unauthorized attempt to delete user with Id {UserId}.", request.Id);
            return Result<bool>.Failure("Unauthorized access.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Delete user failed. Target UserId {TargetUserId} not found.", request.Id);
            return Result<bool>.Failure("User not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            user,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Forbidden user deletion attempt. UserId {ExecutingUserId} tried to delete UserId {TargetUserId}.",
                _currentUserService.UserId, request.Id);
            return Result<bool>.Failure("You are not authorized to delete this user account.");
        }

        user.MarkAsDeleted();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {TargetUserId} soft-deleted successfully by UserId {ExecutingUserId}.",
            user.Id, _currentUserService.UserId);

        return Result<bool>.Success(true);
    }
}