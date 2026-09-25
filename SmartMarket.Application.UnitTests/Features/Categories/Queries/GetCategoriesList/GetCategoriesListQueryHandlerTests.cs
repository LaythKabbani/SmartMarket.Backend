using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMarket.Application.Features.Categories.Dtos;
using SmartMarket.Application.Features.Categories.Queries.GetCategoriesList;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Categories.Queries.GetCategoriesList;

public class GetCategoriesListQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetCategoriesListQueryHandler _handler;

    public GetCategoriesListQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Category, CategoryDto>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();
        _handler = new GetCategoriesListQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnListOfCategories_WhenCategoriesExist()
    {
        // 1. Arrange
        var categories = new List<Category>
        {
            new Category("Beverages", "Drinks and sodas"),
            new Category("Bakery", "Fresh bread and pastries"),
            new Category("Dairy", "Milk and cheese")
        };

        Context.Categories.AddRange(categories);
        await Context.SaveChangesAsync();

        var query = new GetCategoriesListQuery();

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(c => c.Name).Should().Contain(new[] { "Beverages", "Bakery", "Dairy" });
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        // 1. Arrange
        var query = new GetCategoriesListQuery();

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}