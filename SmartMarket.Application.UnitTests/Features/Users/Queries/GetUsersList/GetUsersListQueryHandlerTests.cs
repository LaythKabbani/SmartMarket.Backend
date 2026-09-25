using AutoMapper;
using FluentAssertions;
using SmartMarket.Application.Features.Users.Dtos;
using SmartMarket.Application.Features.Users.Queries.GetUsersList;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Users.Queries.GetUsersList;

public class GetUsersListQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetUsersListQueryHandler _handler;

    public GetUsersListQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserDto>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();
        _handler = new GetUsersListQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedList_WhenNoUsersExist()
    {
        // 1. Arrange
        var query = new GetUsersListQuery(PageNumber: 1, PageSize: 10);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(1);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedUsersList_WhenUsersExist()
    {
        // 1. Arrange: Seed 5 users into DB
        var users = new List<User>
        {
            new User("User 1", "user1@example.com", "Pass1", UserRole.Customer),
            new User("User 2", "user2@example.com", "Pass2", UserRole.Customer),
            new User("User 3", "user3@example.com", "Pass3", UserRole.SuperAdmin),
            new User("User 4", "user4@example.com", "Pass4", UserRole.Customer),
            new User("User 5", "user5@example.com", "Pass5", UserRole.SuperAdmin)
        };

        Context.Users.AddRange(users);
        await Context.SaveChangesAsync();

        var query = new GetUsersListQuery(PageNumber: 1, PageSize: 2);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(1);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnSecondPageCorrectly_WhenRequestingPageTwo()
    {
        // 1. Arrange: Seed 3 users into DB
        var users = new List<User>
        {
            new User("User 1", "user1@example.com", "Pass1", UserRole.Customer),
            new User("User 2", "user2@example.com", "Pass2", UserRole.Customer),
            new User("User 3", "user3@example.com", "Pass3", UserRole.SuperAdmin)
        };

        Context.Users.AddRange(users);
        await Context.SaveChangesAsync();

        var query = new GetUsersListQuery(PageNumber: 2, PageSize: 2);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }
}