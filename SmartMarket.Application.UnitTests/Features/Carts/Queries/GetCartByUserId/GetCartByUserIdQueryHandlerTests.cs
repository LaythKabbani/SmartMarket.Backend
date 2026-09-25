using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMarket.Application.Features.Carts.Dtos;
using SmartMarket.Application.Features.Carts.Queries.GetCartByUserId;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Carts.Queries.GetCartByUserId;

public class GetCartByUserIdQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetCartByUserIdQueryHandler _handler;

    public GetCartByUserIdQueryHandlerTests()
    {
        // Setup real AutoMapper configuration to support ProjectTo
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Cart, CartDto>();
            cfg.CreateMap<CartItem, CartItemDto>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();
        _handler = new GetCartByUserIdQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyCartDto_WhenCartDoesNotExist()
    {
        // 1. Arrange
        var userId = Guid.NewGuid();
        var query = new GetCartByUserIdQuery(userId);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(Guid.Empty);
        result.Data!.UserId.Should().Be(userId);
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedCartDto_WhenCartExists()
    {
        // 1. Arrange
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product("Speciality Coffee", "Single origin", 45.0m, stockQuantity: 20, storeId, categoryId);
        Context.Products.Add(product);

        var cart = new Cart(userId);
        cart.AddOrUpdateItem(product.Id, 2);
        Context.Carts.Add(cart);

        await Context.SaveChangesAsync();

        var query = new GetCartByUserIdQuery(userId);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        result.Data!.Items.Should().HaveCount(1);

        var itemDto = result.Data!.Items.First();
        itemDto.ProductId.Should().Be(product.Id);
        itemDto.Quantity.Should().Be(2);
    }
}