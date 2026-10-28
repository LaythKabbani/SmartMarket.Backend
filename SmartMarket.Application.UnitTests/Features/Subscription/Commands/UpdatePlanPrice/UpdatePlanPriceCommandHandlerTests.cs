using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartMarket.Application.Common.Interfaces;
using SmartMarket.Application.Common.Models;
using SmartMarket.Application.Features.Subscriptions.Commands.UpdatePlanPrice;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using SmartMarket.Domain.Enums;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Subscriptions.Commands.UpdatePlanPrice;

public class UpdatePlanPriceCommandHandlerTests : TestBase
{
    private readonly UpdatePlanPriceCommandHandler _handler;

    public UpdatePlanPriceCommandHandlerTests()
    {
        _handler = new UpdatePlanPriceCommandHandler(Context);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPlanNotFound()
    {
        var command = new UpdatePlanPriceCommand(SubscriptionPlan.Monthly, 150.0m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("was not found");
    }

    [Fact]
    public async Task Handle_ShouldUpdatePlanPrice_WhenPlanExists()
    {
        var subscriptionPlan = new SubscriptionPlanConfig(SubscriptionPlan.Monthly, 100.0m, "USD");
        Context.SubscriptionPlans.Add(subscriptionPlan);
        await Context.SaveChangesAsync();

        var command = new UpdatePlanPriceCommand(SubscriptionPlan.Monthly, 150.0m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        var updatedPlan = await Context.SubscriptionPlans.FindAsync(subscriptionPlan.Id);
        updatedPlan.Should().NotBeNull();
        updatedPlan!.Price.Should().Be(150.0m);
    }
}