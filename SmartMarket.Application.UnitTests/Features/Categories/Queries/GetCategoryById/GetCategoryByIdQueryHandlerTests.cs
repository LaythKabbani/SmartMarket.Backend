using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMarket.Application.Features.Categories.Dtos;
using SmartMarket.Application.Features.Categories.Queries.GetCategoryById;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetCategoryByIdQueryHandler _handler;

    public GetCategoryByIdQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Category, CategoryDto>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();
        _handler = new GetCategoryByIdQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoryDto_WhenCategoryExists()
    {
        // 1. Arrange
        var category = new Category("Electronics", "Gadgets and tech items");
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var query = new GetCategoryByIdQuery(category.Id);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Electronics");
        result.Data.Description.Should().Be("Gadgets and tech items");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        // 1. Arrange
        var query = new GetCategoryByIdQuery(Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Category not found.");
    }
}