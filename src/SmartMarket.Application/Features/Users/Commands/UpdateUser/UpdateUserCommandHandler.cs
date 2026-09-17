using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Domain.Enums;

namespace SmartMarket.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.User == null)
        {
            _logger.LogWarning("Unauthorized attempt to update user profile for TargetUserId {TargetUserId}.", request.Id);
            return Result<bool>.Failure("Unauthorized access.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Update user failed. TargetUserId {TargetUserId} not found.", request.Id);
            return Result<bool>.Failure("User not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User,
            user,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Forbidden attempt to update profile of TargetUserId {TargetUserId} by ExecutingUserId {ExecutingUserId}.",
                request.Id, _currentUserService.UserId);
            return Result<bool>.Failure("You are not authorized to update this user profile.");
        }

        var isSuperAdmin = _currentUserService.User.IsInRole(UserRole.SuperAdmin.ToString());
        var targetRole = isSuperAdmin ? request.Role : user.Role;

        var oldRole = user.Role;
        user.Update(request.FullName, targetRole);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User profile {TargetUserId} updated successfully by ExecutingUserId {ExecutingUserId}. OldRole: {OldRole}, NewRole: {NewRole}",
            user.Id, _currentUserService.UserId, oldRole, targetRole);

        return Result<bool>.Success(true);
    }
}