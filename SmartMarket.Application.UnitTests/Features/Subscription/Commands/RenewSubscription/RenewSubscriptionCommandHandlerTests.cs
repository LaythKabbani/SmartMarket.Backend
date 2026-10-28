using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Common.Security;
using SmartMarket.Application.Features.Subscriptions.Commands.RenewSubscription;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Subscriptions.Commands.RenewSubscription;

public class RenewSubscriptionCommandHandlerTests : TestBase
{
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<ILogger<RenewSubscriptionCommandHandler>> _loggerMock;
    private readonly RenewSubscriptionCommandHandler _handler;

    public RenewSubscriptionCommandHandlerTests()
    {
        _paymentServiceMock = new Mock<IPaymentService>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _loggerMock = new Mock<ILogger<RenewSubscriptionCommandHandler>>();

        _handler = new RenewSubscriptionCommandHandler(
            Context,
            _paymentServiceMock.Object,
            CurrentUserServiceMock.Object,
            _authorizationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new RenewSubscriptionCommand(Guid.NewGuid(), SubscriptionPlan.Monthly, "token_123");

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

        var command = new RenewSubscriptionCommand(Guid.NewGuid(), SubscriptionPlan.Monthly, "token_123");

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

        var user2 = new User("Test user 2", "test2user@market.com", "hashedpassword", UserRole.Merchant);

        SetupUserContext(user2.Id.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", user.Id, "Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Failed());

        var command = new RenewSubscriptionCommand(store.Id, SubscriptionPlan.Monthly, "token_123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenSubscriptionNotFound()
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

        var command = new RenewSubscriptionCommand(store.Id, SubscriptionPlan.Monthly, "token_123");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No subscription found");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPaidPlanHasNoPaymentToken()
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
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            SubscriptionStatus.Expired
        );
        Context.MerchantSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var command = new RenewSubscriptionCommand(store.Id, SubscriptionPlan.Monthly, PaymentToken: null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Payment token is required for renewing paid subscriptions.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPaymentValidationFails()
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
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-1),
            SubscriptionStatus.Expired
        );
        Context.MerchantSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        var paymentToken = "invalid_token";
        _paymentServiceMock
            .Setup(x => x.ValidateTokenAsync(paymentToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Invalid or expired payment token."));

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var command = new RenewSubscriptionCommand(store.Id, SubscriptionPlan.Monthly, paymentToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid or expired payment token.");
        _paymentServiceMock.Verify(x => x.ValidateTokenAsync(paymentToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRenewSubscription_WhenPaidPlanIsValid()
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

        var paymentToken = "valid_token";
        _paymentServiceMock
            .Setup(x => x.ValidateTokenAsync(paymentToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var command = new RenewSubscriptionCommand(store.Id, SubscriptionPlan.Yearly, paymentToken);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        var updatedSubscription = await Context.MerchantSubscriptions.FindAsync(subscription.Id);
        updatedSubscription.Should().NotBeNull();
        updatedSubscription!.PlanType.Should().Be(SubscriptionPlan.Yearly);
        updatedSubscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Handle_ShouldRenewSubscription_WhenDemoPlanIsSelectedWithoutPaymentToken()
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
            DateTime.UtcNow.AddDays(-40),
            DateTime.UtcNow.AddDays(-10),
            SubscriptionStatus.Expired
        );
        Context.MerchantSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.OfType<OwnerOrSuperAdminRequirement>().Any())))
            .ReturnsAsync(AuthorizationResult.Success());

        var command = new RenewSubscriptionCommand(store.Id, SubscriptionPlan.Demo, PaymentToken: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        _paymentServiceMock.Verify(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        var updatedSubscription = await Context.MerchantSubscriptions.FindAsync(subscription.Id);
        updatedSubscription.Should().NotBeNull();
        updatedSubscription!.PlanType.Should().Be(SubscriptionPlan.Demo);
        updatedSubscription.Status.Should().Be(SubscriptionStatus.Active);
    }
}