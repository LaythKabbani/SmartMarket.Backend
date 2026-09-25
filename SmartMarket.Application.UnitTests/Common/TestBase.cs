using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Infrastructure.Persistence;

namespace SmartMarket.Application.UnitTests.Common;

public abstract class TestBase
{
    protected readonly ApplicationDbContext Context;
    protected readonly Mock<IAuthorizationService> AuthorizationServiceMock;
    protected readonly Mock<ICurrentUserService> CurrentUserServiceMock;

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new ApplicationDbContext(options);
        AuthorizationServiceMock = new Mock<IAuthorizationService>();
        CurrentUserServiceMock = new Mock<ICurrentUserService>();
    }

    /// <summary>
    /// setting up the user context for testing purposes. This method configures the mocked current user service and authorization service based on the provided parameters.
    /// </summary>
    protected ClaimsPrincipal? SetupUserContext(
        string? userId,
        string? role = null,
        bool isAuthenticated = true,
        bool isAuthorized = true)
    {
        if (!isAuthenticated || string.IsNullOrEmpty(userId))
        {
            CurrentUserServiceMock.Setup(x => x.User).Returns((ClaimsPrincipal?)null);
            CurrentUserServiceMock.Setup(x => x.UserId).Returns((string?)null);
            return null;
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        CurrentUserServiceMock.Setup(x => x.User).Returns(claimsPrincipal);
        CurrentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        AuthorizationServiceMock
            .Setup(x => x.AuthorizeAsync(
                claimsPrincipal,
                It.IsAny<object>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(isAuthorized ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        return claimsPrincipal;
    }
}