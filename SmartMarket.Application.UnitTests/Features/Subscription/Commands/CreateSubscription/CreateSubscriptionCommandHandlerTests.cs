using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Commands.CreateSubscription;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Subscriptions.Commands.CreateSubscription;

public class CreateSubscriptionCommandHandlerTests : TestBase
{
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<ILogger<CreateSubscriptionCommandHandler>> _loggerMock;
    private readonly CreateSubscriptionCommandHandler _handler;

    public CreateSubscriptionCommandHandlerTests()
    {
        _paymentServiceMock = new Mock<IPaymentService>();
        _loggerMock = new Mock<ILogger<CreateSubscriptionCommandHandler>>();

        _handler = new CreateSubscriptionCommandHandler(
            Context,
            _paymentServiceMock.Object,
            CurrentUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        SetupUserContext(userId: null, isAuthenticated: false);

        var command = new CreateSubscriptionCommand(Guid.NewGuid(), SubscriptionPlan.Monthly, "token_123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Unauthorized access. User is not authenticated.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStoreAlreadyHasActiveSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", userId, "Test Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var activeSubscription = new MerchantSubscription(
            store.Id,
            SubscriptionPlan.Monthly,
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(25),
            SubscriptionStatus.Active
        );
        Context.MerchantSubscriptions.Add(activeSubscription);
        await Context.SaveChangesAsync();

        var command = new CreateSubscriptionCommand(store.Id, SubscriptionPlan.Yearly, "token_123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("This store already has an active subscription.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPaidPlanHasNoPaymentToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", userId, "Test Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var command = new CreateSubscriptionCommand(store.Id, SubscriptionPlan.Monthly, PaymentToken: null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Payment token is required for paid subscription plans.");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPaymentValidationFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupUserContext(userId.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", userId, "Test Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var paymentToken = "invalid_token";
        _paymentServiceMock
            .Setup(x => x.ValidateTokenAsync(paymentToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure("Invalid or expired payment token."));

        var command = new CreateSubscriptionCommand(store.Id, SubscriptionPlan.Monthly, paymentToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid or expired payment token.");
        _paymentServiceMock.Verify(x => x.ValidateTokenAsync(paymentToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateSubscriptionAndUpgradeUser_WhenPaidPlanIsValid()
    {
        // Arrange
        var user = new User("Test User", "test@market.com", "password1234", UserRole.Customer);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", user.Id, "Test Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var paymentToken = "valid_token";
        _paymentServiceMock
            .Setup(x => x.ValidateTokenAsync(paymentToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        var command = new CreateSubscriptionCommand(store.Id, SubscriptionPlan.Monthly, paymentToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var createdSubscription = await Context.MerchantSubscriptions.FindAsync(result.Data);
        createdSubscription.Should().NotBeNull();
        createdSubscription!.StoreId.Should().Be(store.Id);
        createdSubscription!.PlanType.Should().Be(SubscriptionPlan.Monthly);
        createdSubscription!.Status.Should().Be(SubscriptionStatus.Active);

        var updatedUser = await Context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Role.Should().Be(UserRole.Merchant);
    }

    [Fact]
    public async Task Handle_ShouldCreateSubscription_WhenDemoPlanIsSelectedWithoutPaymentToken()
    {
        // Arrange
        var user = new User("Demo User", "demo@market.com", "password1234", UserRole.Customer);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetupUserContext(user.Id.ToString(), isAuthenticated: true);

        var store = new Store("Test Store", user.Id, "Test Description", "http://logo.url");
        Context.Stores.Add(store);
        await Context.SaveChangesAsync();

        var command = new CreateSubscriptionCommand(store.Id, SubscriptionPlan.Demo, PaymentToken: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        _paymentServiceMock.Verify(x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        var createdSubscription = await Context.MerchantSubscriptions.FindAsync(result.Data);
        createdSubscription.Should().NotBeNull();
        createdSubscription!.PlanType.Should().Be(SubscriptionPlan.Demo);
    }
}