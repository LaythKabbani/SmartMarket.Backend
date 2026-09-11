using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthResponse>>;