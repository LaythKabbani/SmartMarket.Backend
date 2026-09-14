using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Auth.Commands.ForgotPassword;
using SmartMarket.Application.Features.Auth.Commands.Login;
using SmartMarket.Application.Features.Auth.Commands.Logout;
using SmartMarket.Application.Features.Auth.Commands.RefreshToken;
using SmartMarket.Application.Features.Auth.Commands.Register;
using SmartMarket.Application.Features.Auth.Commands.ResetPassword;
using SmartMarket.Domain.Enums;

namespace SmartMarket.WebApi.Controllers;

[Authorize]
public class AuthController : ApiControllerBase
{
    // POST: api/auth/register
    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("AuthRateLimit")]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<Result<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.FullName,
            request.Email,
            request.Password,
            UserRole.Customer
        );

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // POST: api/auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("LoginRateLimit")]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<Result<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // POST: api/auth/refresh-token
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<Result<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // POST: api/auth/logout
    [HttpPost("logout")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<Result<bool>>> Logout([FromBody] LogoutRequest request)
    {
        if (!Guid.TryParse(CurrentUserId, out var userId))
            return Unauthorized();

        var command = new LogoutCommand(userId, request.RefreshToken);

        var result = await Mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // POST: api/auth/forgot-password
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthRateLimit")]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await Mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { message = "If the email is registered, a password reset OTP has been generated." })
            : BadRequest(result);
    }

    // POST: api/auth/reset-password
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting("AuthRateLimit")]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AuthResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await Mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { message = "Password has been reset successfully." })
            : BadRequest(result);
    }
}