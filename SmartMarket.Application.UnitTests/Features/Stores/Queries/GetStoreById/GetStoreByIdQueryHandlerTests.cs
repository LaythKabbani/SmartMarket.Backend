using AutoMapper;
using FluentAssertions;
using SmartMarket.Application.Features.Stores.Dtos;
using SmartMarket.Application.Features.Stores.Queries.GetStoreById;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Stores.Queries.GetStoreById;

public class GetStoreByIdQueryHandlerTests : TestBase
{
    private readonly IMapper _mapper;
    private readonly GetStoreByIdQueryHandler _handler;

    public GetStoreByIdQueryHandlerTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Store, StoreDto>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();
        _handler = new GetStoreByIdQueryHandler(Context, _mapper);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStoreDoesNotExist()
    {
        // 1. Arrange: Send query with a non-existent Store Id
        var query = new GetStoreByIdQuery(Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Store not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnStoreDto_WhenStoreExists()
    {
        // 1. Arrange: Create and save a store in DB
        var ownerId = Guid.NewGuid();
        var store = new Store("My Store", ownerId, "Store Description", "logo.png");

        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var query = new GetStoreByIdQuery(store.Id);

        // 2. Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(store.Id);
        result.Data.Name.Should().Be("My Store");
        result.Data.Description.Should().Be("Store Description");
        result.Data.LogoUrl.Should().Be("logo.png");
        result.Data.OwnerId.Should().Be(ownerId);
    }
}