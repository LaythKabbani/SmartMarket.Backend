using MediatR;
using SmartMarket.Application.Common.Models;

namespace SmartMarket.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result<bool>>;