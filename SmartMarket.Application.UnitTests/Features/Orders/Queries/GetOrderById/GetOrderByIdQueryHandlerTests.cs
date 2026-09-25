using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMarket.Application.Common.Mappings;
using SmartMarket.Application.Features.Orders.Queries.GetOrderById;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();

        _handler = new GetOrderByIdQueryHandler(
            Context,
            _mapper,
            AuthorizationServiceMock.Object,
            CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetupUserContext(userId: null, isAuthenticated: false);

        var query = new GetOrderByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenOrderDoesNotExist()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString());

        var query = new GetOrderByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthorized()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: false);

        var order = new Order(Guid.NewGuid(), 100m, "Damascus, Syria");

        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var query = new GetOrderByIdQuery(order.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found or access denied.");
    }

    [Fact]
    public async Task Handle_ShouldReturnOrderDto_WhenUserIsAuthorizedAndOrderExists()
    {
        // Arrange
        SetupUserContext(Guid.NewGuid().ToString(), isAuthorized: true);

        var ownerId = Guid.NewGuid();

        var store = new Store("Smart Store", ownerId, "Tech Store", "https://logo.url");
        var category = new Category("Electronics", "Gadgets");
        var product = new Product("Laptop", "Tech", 1000m, stockQuantity: 5, store.Id, category.Id);

        Context.Stores.Add(store);
        Context.Categories.Add(category);
        Context.Products.Add(product);

        var order = new Order(Guid.NewGuid(), 1000m, "Damascus, Syria");
        var item = new OrderItem(order.Id, product.Id, quantity: 1, unitPrice: 1000m);
        order.Items.Add(item);

        Context.Orders.Add(order);
        await Context.SaveChangesAsync();

        var query = new GetOrderByIdQuery(order.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(order.Id);
        result.Data.TotalAmount.Should().Be(order.TotalAmount);
    }
}