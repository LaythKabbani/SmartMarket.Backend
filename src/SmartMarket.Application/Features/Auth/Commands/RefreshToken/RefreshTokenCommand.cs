using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<Result<AuthResponse>>;