using Application.Behaviors;
using Application.Commons;
using Cortex.Mediator.Commands;
using FluentValidation;
using Tests.Shared.Attributes;
using Tests.Shared.Base;

namespace Tests.Application.Behaviors;

[Unit]
public sealed class ValidationBehaviorTests : UnitTestsBase
{
    private sealed record TestCommand(string Name) : ICommand<OperationResult<string>>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator() =>
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
    }

    [Test]
    public async Task ValidationCommandBehavior_returns_Fail_Validation_when_invalid_without_throwing()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var sut = new ValidationCommandBehavior<TestCommand, OperationResult<string>>(validators);

        // Act
        var result = await sut.Handle(
            new TestCommand(""),
            () => Task.FromResult(OperationResult<string>.SuccessResult("ok")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Validation);
        result.Errors.Should().NotBeEmpty();
    }

    [Test]
    public async Task ValidationCommandBehavior_proceeds_to_handler_when_valid()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var sut = new ValidationCommandBehavior<TestCommand, OperationResult<string>>(validators);

        // Act
        var result = await sut.Handle(
            new TestCommand("Valid Name"),
            () => Task.FromResult(OperationResult<string>.SuccessResult("ok")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("ok");
    }

    [Test]
    public async Task ValidationCommandBehavior_with_no_validators_proceeds_to_handler()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var sut = new ValidationCommandBehavior<TestCommand, OperationResult<string>>(validators);

        // Act
        var result = await sut.Handle(
            new TestCommand(""),
            () => Task.FromResult(OperationResult<string>.SuccessResult("ok")),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task ValidationCommandBehavior_collects_all_errors_from_failures()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var sut = new ValidationCommandBehavior<TestCommand, OperationResult<string>>(validators);

        // Act
        var result = await sut.Handle(
            new TestCommand(""),
            () => Task.FromResult(OperationResult<string>.SuccessResult("ok")),
            CancellationToken.None);

        // Assert
        result.ErrorCode.Should().Be(ErrorCode.Validation);
        result.Errors.Should().Contain("Name is required.");
    }
}
