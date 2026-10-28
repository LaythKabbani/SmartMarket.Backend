using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Security;
using SmartMarket.Application.Features.Subscriptions.Commands.CancelSubscription;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Subscriptions.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandlerTests : TestBase
{
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<ILogger<CancelSubscriptionCommandHandler>> _loggerMock;
    private readonly CancelSubscriptionCommandHandler _handler;

    public CancelSubscriptionCommandHandlerTests()
    {
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _loggerMock = new Mock<ILogger<CancelSubscriptionCommandHandler>>();

        _handler = new CancelSubscriptionCommandHandler(
            Context,
            CurrentUserServiceMock.Object,
            _authorizationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new CancelSubscriptionCommand(Guid.NewGuid(), "User requested cancellation");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access. User is not authenticated.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStoreNotFound()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Merchant);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var command = new CancelSubscriptionCommand(Guid.NewGuid(), "User requested cancellation");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("was not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAuthorizationFails()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Merchant);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", user.Id, "Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Failed());

        var command = new CancelSubscriptionCommand(store.Id, "User requested cancellation");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNoActiveSubscriptionFound()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Merchant);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", user.Id, "Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var command = new CancelSubscriptionCommand(store.Id, "User requested cancellation");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No active subscription found");
    }

    [Fact]
    public async Task Handle_ShouldCancelSubscription_WhenRequestIsValid()
    {
        var user = new User("Test User", "testuser@market.com", "hashedpassword", UserRole.Merchant);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", user.Id, "Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var subscription = new MerchantSubscription(
            store.Id,
            SubscriptionPlan.Monthly,
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(20),
            SubscriptionStatus.Active
        );
        Context.MerchantSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var command = new CancelSubscriptionCommand(store.Id, "User requested cancellation");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        var updatedSubscription = await Context.MerchantSubscriptions.FindAsync(subscription.Id);
        updatedSubscription.Should().NotBeNull();
        updatedSubscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
    }
}