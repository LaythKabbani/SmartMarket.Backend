using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Features.Categories.Commands.DeleteCategory;
using SmartMarket.Application.UnitTests.Common;
using SmartMarket.Domain.Entities;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandlerTests : TestBase
{
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _handler = new DeleteCategoryCommandHandler(Context);
    }

    [Fact]
    public async Task Handle_ShouldDeleteCategorySuccessfully_WhenCategoryExists()
    {
        // 1. Arrange
        var category = new Category("Electronics", "Gadgets and devices");
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var command = new DeleteCategoryCommand(category.Id);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();

        // Verification in DB
        Context.ChangeTracker.Clear();

        var categoryInDb = await Context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == category.Id);

        categoryInDb.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        // 1. Arrange
        var command = new DeleteCategoryCommand(Guid.NewGuid());

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Category not found.");
    }
}