using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Features.Categories.Commands.UpdateCategory;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandlerTests : TestBase
{
    private readonly UpdateCategoryCommandHandler _handler;

    public UpdateCategoryCommandHandlerTests()
    {
        _handler = new UpdateCategoryCommandHandler(Context);
    }

    [Fact]
    public async Task Handle_ShouldUpdateCategorySuccessfully_WhenCategoryExists()
    {
        // 1. Arrange
        var category = new Category("Old Name", "Old Description");
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();

        var command = new UpdateCategoryCommand(category.Id, "Updated Name", "Updated Description");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Verification in DB
        Context.ChangeTracker.Clear();

        var updatedCategoryInDb = await Context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == category.Id);

        updatedCategoryInDb.Should().NotBeNull();
        updatedCategoryInDb!.Name.Should().Be("Updated Name");
        updatedCategoryInDb!.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        // 1. Arrange
        var command = new UpdateCategoryCommand(Guid.NewGuid(), "New Name", "New Description");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Category not found.");
    }
}