using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMarket.Application.Common.Mappings;
using SmartMarket.Application.Features.Products.Dtos;
using SmartMarket.Application.Features.Products.Queries.GetProductsList;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Products.Queries.GetProductsList;

public class GetProductsListQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetProductsListQueryHandler _handler;

    public GetProductsListQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = mapperConfig.CreateMapper();

        _handler = new GetProductsListQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllProducts_WhenNoFiltersApplied()
    {
        // Arrange
        await SeedDatabaseAsync();

        var query = new GetProductsListQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldFilterByStoreId_WhenStoreIdIsProvided()
    {
        // Arrange
        var (store1Id, _, _, _) = await SeedDatabaseAsync();

        var query = new GetProductsListQuery
        {
            StoreId = store1Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.StoreId.Should().Be(store1Id));
    }

    [Fact]
    public async Task Handle_ShouldFilterByCategoryId_WhenCategoryIdIsProvided()
    {
        // Arrange
        var (_, _, category1Id, _) = await SeedDatabaseAsync();

        var query = new GetProductsListQuery
        {
            CategoryId = category1Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.CategoryId.Should().Be(category1Id));
    }

    [Fact]
    public async Task Handle_ShouldFilterByBothStoreIdAndCategoryId_WhenBothAreProvided()
    {
        // Arrange
        var (store1Id, _, category1Id, _) = await SeedDatabaseAsync();

        var query = new GetProductsListQuery
        {
            StoreId = store1Id,
            CategoryId = category1Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Laptop");
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResult_WhenPageSizeIsSmallerThanTotalCount()
    {
        // Arrange
        await SeedDatabaseAsync();

        var query = new GetProductsListQuery
        {
            PageNumber = 1,
            PageSize = 2
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoProductsMatchFilter()
    {
        // Arrange
        await SeedDatabaseAsync();

        var query = new GetProductsListQuery
        {
            StoreId = Guid.NewGuid(),
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    private async Task<(Guid Store1Id, Guid Store2Id, Guid Category1Id, Guid Category2Id)> SeedDatabaseAsync()
    {
        var store1 = new Store("Tech World", Guid.NewGuid(), "Electronics Store");
        var store2 = new Store("Fashion Hub", Guid.NewGuid(), "Clothing Store");

        var category1 = new Category("Electronics", "Gadgets and Devices");
        var category2 = new Category("Fashion", "Apparel and Accessories");

        Context.Stores.AddRange(store1, store2);
        Context.Categories.AddRange(category1, category2);

        var product1 = new Product("Laptop", "Gaming Laptop", 1200m, 10, store1.Id, category1.Id);
        var product2 = new Product("Mouse", "Wireless Mouse", 25m, 50, store1.Id, category2.Id);
        var product3 = new Product("Shirt", "Cotton T-Shirt", 15m, 100, store2.Id, category1.Id);

        Context.Products.AddRange(product1, product2, product3);
        await Context.SaveChangesAsync();

        return (store1.Id, store2.Id, category1.Id, category2.Id);
    }
}