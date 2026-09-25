using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartMarket.Application.Features.Categories.Commands.CreateCategory;
using SmartMarket.Application.UnitTests.Common;
using Xunit;

namespace SmartMarket.Application.UnitTests.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandlerTests : TestBase
{
    private readonly CreateCategoryCommandHandler _handler;
    private readonly CreateCategoryCommandValidator _validator;

    public CreateCategoryCommandHandlerTests()
    {
        _handler = new CreateCategoryCommandHandler(Context);
        _validator = new CreateCategoryCommandValidator();
    }

    [Fact]
    public async Task Handle_ShouldCreateCategorySuccessfully_WhenCommandIsValid()
    {
        // 1. Arrange
        var command = new CreateCategoryCommand("Beverages", "Soft drinks and juices");

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        // Verification in DB
        Context.ChangeTracker.Clear();

        var categoryInDb = await Context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == result.Data);

        categoryInDb.Should().NotBeNull();
        categoryInDb!.Name.Should().Be("Beverages");
        categoryInDb!.Description.Should().Be("Soft drinks and juices");
    }

    [Fact]
    public async Task Handle_ShouldCreateCategoryWithEmptyDescription_WhenDescriptionIsNotProvided()
    {
        // 1. Arrange
        var command = new CreateCategoryCommand("Dairy", string.Empty);

        // 2. Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // 3. Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        // Verification in DB
        Context.ChangeTracker.Clear();

        var categoryInDb = await Context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == result.Data);

        categoryInDb.Should().NotBeNull();
        categoryInDb!.Name.Should().Be("Dairy");
        categoryInDb!.Description.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldHaveValidationError_WhenNameIsEmpty()
    {
        // 1. Arrange
        var command = new CreateCategoryCommand(string.Empty, "Some description");

        // 2. Act
        var result = _validator.Validate(command);

        // 3. Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateCategoryCommand.Name));
    }
}