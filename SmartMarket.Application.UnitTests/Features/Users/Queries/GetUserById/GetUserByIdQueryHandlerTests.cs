using AutoMapper;
using FluentAssertions;
using SmartMarket.Application.Features.Users.Dtos;
using SmartMarket.Application.Features.Users.Queries.GetUserById;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();

        _handler = new GetUserByIdQueryHandler(
            Context,
            _mapper,
            AuthorizationServiceMock.Object,
            CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        // 1. Arrange: Send query with a non-existent User Id
        SetupUserContext(userId: Guid.NewGuid().ToString());

        var query = new GetUserByIdQuery(Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorizedByPolicy()
    {
        // 1. Arrange: Executing user is authenticated but NOT authorized to view target user (isAuthorized: false)
        var executingUserId = Guid.NewGuid().ToString();
        SetupUserContext(userId: executingUserId, isAuthorized: false);

        var targetUser = new User("Target User", "target@example.com", "HashedPass123", UserRole.Customer);
        Context.Users.Add(targetUser);
        await Context.SaveChangesAsync();

        var query = new GetUserByIdQuery(targetUser.Id);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("You are not authorized to view this user profile.");
    }

    [Fact]
    public async Task Handle_ShouldReturnUserDto_WhenUserExistsAndAuthorizationSucceeds()
    {
        // 1. Arrange: Executing user is authenticated AND authorized (isAuthorized: true)
        var executingUserId = Guid.NewGuid().ToString();
        SetupUserContext(userId: executingUserId, isAuthorized: true);

        var targetUser = new User("Target User", "target@example.com", "HashedPass123", UserRole.SuperAdmin);
        Context.Users.Add(targetUser);
        await Context.SaveChangesAsync();

        var query = new GetUserByIdQuery(targetUser.Id);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(targetUser.Id);
        result.Data.FullName.Should().Be("Target User");
        result.Data.Email.Should().Be("target@example.com");
        result.Data.Role.Should().Be(UserRole.SuperAdmin);
    }
}