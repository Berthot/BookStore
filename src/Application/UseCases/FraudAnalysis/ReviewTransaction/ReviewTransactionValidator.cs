using FluentValidation;

namespace Application.UseCases.FraudAnalysis.ReviewTransaction;

/// <summary>Validates the review request before any database access — fires via ValidationCommandBehavior.</summary>
internal sealed class ReviewTransactionValidator : AbstractValidator<ReviewTransactionRequest>
{
    private static readonly string[] ValidOutcomes = ["APPROVED", "REJECTED"];

    public ReviewTransactionValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty()
            .WithMessage("Justification is required.");

        RuleFor(x => x.Outcome)
            .Must(o => o is not null && ValidOutcomes.Contains(o.ToUpperInvariant()))
            .WithMessage("Outcome must be APPROVED or REJECTED.");
    }
}
