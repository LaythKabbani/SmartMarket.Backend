using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Users.Commands.CreateUser;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandlerTests : TestBase
{
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<CreateUserCommandHandler>> _loggerMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<CreateUserCommandHandler>>();

        _handler = new CreateUserCommandHandler(
            Context,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        // 1. Arrange: Seed an existing user with the same email
        var existingUser = new User("Existing User", "duplicate@example.com", "HashedPass123", UserRole.Customer);
        Context.Users.Add(existingUser);
        await Context.SaveChangesAsync();

        var command = new CreateUserCommand("New User", "duplicate@example.com", "Password123!", UserRole.Customer);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("A user with this email address already exists.");
    }

    [Fact]
    public async Task Handle_ShouldCreateUserAndReturnUserId_WhenDataIsValid()
    {
        // 1. Arrange: Setup password hasher mock
        _passwordHasherMock
            .Setup(h => h.HashPassword("Password123!"))
            .Returns("HashedPassword123!");

        var command = new CreateUserCommand("Ahmad Al-Mowafak", "ahmad@example.com", "Password123!", UserRole.SuperAdmin);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        // Database Verification: Ensure the user is correctly persisted in DB
        var userInDb = await Context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == result.Data);

        userInDb.Should().NotBeNull();
        userInDb!.FullName.Should().Be("Ahmad Al-Mowafak");
        userInDb.Email.Should().Be("ahmad@example.com");
        userInDb.PasswordHash.Should().Be("HashedPassword123!");
        userInDb.Role.Should().Be(UserRole.SuperAdmin);
    }
}