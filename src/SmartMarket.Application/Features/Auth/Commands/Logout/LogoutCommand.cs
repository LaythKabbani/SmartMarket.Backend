using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(Guid UserId, string RefreshToken) : IRequest<Result<bool>>;