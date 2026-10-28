using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Features.Subscriptions.Queries.GetSubscriptionByStoreId;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Subscriptions.Queries.GetSubscriptionByStoreId;

public class GetSubscriptionByStoreIdQueryHandlerTests : TestBase
{
    private readonly Mock<ILogger<GetSubscriptionByStoreIdQueryHandler>> _loggerMock;
    private readonly GetSubscriptionByStoreIdQueryHandler _handler;

    public GetSubscriptionByStoreIdQueryHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GetSubscriptionByStoreIdQueryHandler>>();

        _handler = new GetSubscriptionByStoreIdQueryHandler(
            Context,
            CurrentUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        SetupUserContext(userId: null, isAuthenticated: false);

        var query = new GetSubscriptionByStoreIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access. User is not authenticated.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenSubscriptionNotFound()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Merchant);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var query = new GetSubscriptionByStoreIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No subscription record found");
    }

    [Fact]
    public async Task Handle_ShouldReturnSubscription_WhenRecordExists()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Merchant);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", user.Id, "Description", "http://logo.url");
        Context.Stores.Add(store);

        var subscriptionPlan = new SubscriptionPlanConfig(SubscriptionPlan.Monthly, 100.0m, "USD");
        Context.SubscriptionPlans.Add(subscriptionPlan);

        var subscription = new MerchantSubscription(
            store.Id,
            SubscriptionPlan.Monthly,
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(20),
            SubscriptionStatus.Active
        );
        Context.MerchantSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        var query = new GetSubscriptionByStoreIdQuery(store.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.StoreId.Should().Be(store.Id);
        result.Data.Plan.Should().Be(SubscriptionPlan.Monthly);
        result.Data.Price.Should().Be(100.0m);
        result.Data.Status.Should().Be(SubscriptionStatus.Active);
    }
}