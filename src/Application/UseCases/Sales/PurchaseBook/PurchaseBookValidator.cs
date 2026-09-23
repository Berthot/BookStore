using FluentValidation;

namespace Application.UseCases.Sales.PurchaseBook;

public sealed class PurchaseBookValidator : AbstractValidator<PurchaseBookRequest>
{
    public PurchaseBookValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PaymentType).NotEmpty();
        RuleFor(x => x.PaymentFingerprint).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
