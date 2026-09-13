using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Application.Features.Users.Dtos;

namespace SmartMarket.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUserService _currentUserService;

    public GetUserByIdQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IAuthorizationService authorizationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _authorizationService = authorizationService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var userEntity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (userEntity == null)
        {
            return Result<UserDto>.Failure("User not found.");
        }

        var authResult = await _authorizationService.AuthorizeAsync(
            _currentUserService.User!,
            userEntity,
            new OwnerOrSuperAdminRequirement()
        );

        if (!authResult.Succeeded)
        {
            return Result<UserDto>.Failure("You are not authorized to view this user profile.");
        }

        var userDto = _mapper.Map<UserDto>(userEntity);
        return Result<UserDto>.Success(userDto);
    }
}