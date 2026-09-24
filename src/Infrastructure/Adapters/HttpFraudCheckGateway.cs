using System.Net.Http.Json;
using Application.UseCases.Sales.Ports;
using Domain.Enums;

namespace Infrastructure.Adapters;

/// <summary>Submits transactions to Fraud.Api over HTTP using a typed HttpClient with standard resilience.</summary>
public sealed class HttpFraudCheckGateway(HttpClient httpClient) : IFraudCheckGateway
{
    public async Task<Guid> SubmitAsync(
        Guid purchaseId,
        string bookFormat,
        int quantity,
        decimal totalAmount,
        string currency,
        string paymentType,
        string paymentFingerprint,
        string? paymentLast4,
        string customerId,
        string correlationId,
        DateTime placedAt,
        CancellationToken cancellationToken = default)
    {
        var delivery = bookFormat.Equals("EBOOK", StringComparison.OrdinalIgnoreCase)
            ? DeliveryType.Digital.ToString()
            : DeliveryType.Physical.ToString();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/transactions")
        {
            Content = JsonContent.Create(new
            {
                externalReference = purchaseId.ToString(),
                customerId,
                amount = new { value = totalAmount, currency },
                payment = new { type = paymentType, fingerprint = paymentFingerprint, last4 = paymentLast4 },
                channel = "API",
                delivery,
                itemCount = quantity,
                occurredAt = placedAt.ToString("O")
            })
        };

        // Idempotency-Key = PurchaseId ensures the same purchase is not submitted twice (ADR-0004 point 4)
        request.Headers.Add("Idempotency-Key", purchaseId.ToString());
        if (!string.IsNullOrEmpty(correlationId))
            request.Headers.Add("X-Correlation-Id", correlationId);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        // 202 covers both the new submission and an idempotency replay — both return the same body
        if ((int)response.StatusCode == 202)
        {
            var body = await response.Content.ReadFromJsonAsync<SubmitTransactionResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Fraud.Api returned 202 with no parseable body.");
            return body.TransactionId;
        }

        throw new InvalidOperationException(
            $"Fraud.Api returned unexpected status {(int)response.StatusCode} for purchase {purchaseId}.");
    }

    private sealed record SubmitTransactionResponse(Guid TransactionId, string Status);
}
