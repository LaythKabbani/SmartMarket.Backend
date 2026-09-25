using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMarket.Application.Common.Mappings;
using SmartMarket.Application.Features.Products.Queries.GetProductById;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Products.Queries.GetProductById;

public class GetProductByIdQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();
        _handler = new GetProductByIdQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductDoesNotExist()
    {
        // Arrange
        var query = new GetProductByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Product not found.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDto_WhenProductExists()
    {
        // Arrange
        var owner = new User("Fullname", "example@gmail.com", "PasswordHash123!", Domain.Enums.UserRole.Merchant);
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var store = new Store("Smart Store", owner.Id, "Tech Store");
        var category = new Category("Electronics", "Gadgets");

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var product = new Product("Laptop", "Gaming Laptop", 1200m, 10, store.Id, category.Id);
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var query = new GetProductByIdQuery(product.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(product.Id);
        result.Data.Name.Should().Be("Laptop");
        result.Data.Price.Should().Be(1200m);
    }
}