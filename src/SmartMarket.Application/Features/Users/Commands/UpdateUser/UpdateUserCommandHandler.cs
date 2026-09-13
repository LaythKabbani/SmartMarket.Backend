using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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

    public UpdateUserCommandHandler(
        IApplicationDbContext context,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
            return Result<bool>.Failure("User not found.");

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User!,
            user,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            return Result<bool>.Failure("You are not authorized to update this user profile.");
        }

        var isSuperAdmin = _currentUserService.User!.IsInRole(UserRole.SuperAdmin.ToString());
        var targetRole = isSuperAdmin ? request.Role : user.Role;

        user.Update(request.FullName, targetRole);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}