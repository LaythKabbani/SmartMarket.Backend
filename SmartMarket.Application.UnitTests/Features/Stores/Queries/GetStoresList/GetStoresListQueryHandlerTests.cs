using AutoMapper;
using FluentAssertions;
using SmartMarket.Application.Features.Stores.Dtos;
using SmartMarket.Application.Features.Stores.Queries.GetStoresList;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Stores.Queries.GetStoresList;

public class GetStoresListQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetStoresListQueryHandler _handler;

    public GetStoresListQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Store, StoreDto>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();
        _handler = new GetStoresListQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedList_WhenNoStoresExist()
    {
        // 1. Arrange
        var query = new GetStoresListQuery(PageNumber: 1, PageSize: 10);

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
    public async Task Handle_ShouldReturnPaginatedStoresList_WhenStoresExist()
    {
        // 1. Arrange: Seed 5 stores into DB
        var ownerId = Guid.NewGuid();
        var stores = new List<Store>
        {
            new Store("Store 1", ownerId, "Desc 1", "logo1.png"),
            new Store("Store 2", ownerId, "Desc 2", "logo2.png"),
            new Store("Store 3", ownerId, "Desc 3", "logo3.png"),
            new Store("Store 4", ownerId, "Desc 4", "logo4.png"),
            new Store("Store 5", ownerId, "Desc 5", "logo5.png")
        };

        Context.Stores.AddRange(stores);
        await Context.SaveChangesAsync();

        var query = new GetStoresListQuery(PageNumber: 1, PageSize: 2);

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
        // 1. Arrange: Seed 3 stores into DB
        var ownerId = Guid.NewGuid();
        var stores = new List<Store>
        {
            new Store("Store 1", ownerId, "Desc 1", "logo1.png"),
            new Store("Store 2", ownerId, "Desc 2", "logo2.png"),
            new Store("Store 3", ownerId, "Desc 3", "logo3.png")
        };

        Context.Stores.AddRange(stores);
        await Context.SaveChangesAsync();

        var query = new GetStoresListQuery(PageNumber: 2, PageSize: 2);

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