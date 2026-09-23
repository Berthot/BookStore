using Domain.Enums;
using FluentValidation;

namespace Application.UseCases.FraudAnalysis.SubmitTransaction;

internal sealed class SubmitTransactionValidator : AbstractValidator<SubmitTransactionRequest>
{
    public SubmitTransactionValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required.");
        RuleFor(x => x.AmountValue).GreaterThan(0).WithMessage("Amount value must be greater than zero.");
        RuleFor(x => x.AmountCurrency).NotEmpty().WithMessage("Amount currency is required.");
        RuleFor(x => x.PaymentFingerprint).NotEmpty().WithMessage("Payment fingerprint is required.");
        RuleFor(x => x.ItemCount).GreaterThan(0).WithMessage("ItemCount must be greater than zero.");
        RuleFor(x => x.CorrelationId).NotEmpty().WithMessage("CorrelationId is required.");
        RuleFor(x => x.Channel)
            .Must(c => Enum.TryParse<Channel>(c, ignoreCase: true, out _))
            .WithMessage("Channel must be WEB, MOBILE or API.");
        RuleFor(x => x.Delivery)
            .Must(d => Enum.TryParse<DeliveryType>(d, ignoreCase: true, out _))
            .WithMessage("Delivery must be DIGITAL or PHYSICAL.");
    }
}
