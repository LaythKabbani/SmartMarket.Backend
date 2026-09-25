using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMarket.Application.Common.Mappings;
using SmartMarket.Application.Features.Orders.Queries.GetUserOrders;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Orders.Queries.GetUserOrders;

public class GetUserOrdersQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetUserOrdersQueryHandler _handler;

    public GetUserOrdersQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();

        _handler = new GetUserOrdersQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserOrdersQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnUserOrdersOrderedByCreatedAtDescending_WhenOrdersExist()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var order1 = new Order(targetUserId, 100m, "Damascus, Syria");
        var order2 = new Order(targetUserId, 200m, "Aleppo, Syria");
        var otherUserOrder = new Order(otherUserId, 300m, "Homs, Syria");

        Context.Orders.AddRange(order1, order2, otherUserOrder);
        await Context.SaveChangesAsync();

        var query = new GetUserOrdersQuery(targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);

        result.Data.Select(o => o.Id).Should().ContainInOrder(order2.Id, order1.Id);
        result.Data.Should().NotContain(o => o.Id == otherUserOrder.Id);
    }
}