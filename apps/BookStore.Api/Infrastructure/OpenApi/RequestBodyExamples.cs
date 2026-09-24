using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace BookStore.Api.Infrastructure.OpenApi;

internal static class RequestBodyExamples
{
    internal static IEndpointConventionBuilder WithPurchaseExamples(this IEndpointConventionBuilder builder)
        => builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            if (operation.RequestBody?.Content is not { } content
                || !content.TryGetValue("application/json", out var media))
                return Task.CompletedTask;

            media.Examples = new Dictionary<string, IOpenApiExample>
            {
                ["s1-approved"] = new OpenApiExample
                {
                    Summary = "S1 — Clean Code físico, cliente estabelecido → APPROVED",
                    Value = JsonNode.Parse("""
                        {
                          "bookId": "11111111-0000-0000-0000-000000000001",
                          "quantity": 1,
                          "customerId": "cus_s1_established",
                          "payment": { "type": "CREDIT_CARD", "fingerprint": "fp_s1_physical_low", "last4": "1234" }
                        }
                        """)
                },
                ["s2-rejected"] = new OpenApiExample
                {
                    Summary = "S2 — Pragmatic Programmer (e-book R$349.90×2), cliente novo → REJECTED (troque customerId e fingerprint por valores nunca vistos)",
                    Value = JsonNode.Parse("""
                        {
                          "bookId": "11111111-0000-0000-0000-000000000002",
                          "quantity": 2,
                          "customerId": "cus_s2_new_demo",
                          "payment": { "type": "CREDIT_CARD", "fingerprint": "fp_s2_new_demo", "last4": "9999" }
                        }
                        """)
                },
                ["s3-review"] = new OpenApiExample
                {
                    Summary = "S3 — Clean Code ×10 (BulkQuantityRule) → UNDER_REVIEW (troque fingerprint por valor nunca visto para evitar CARD_VELOCITY)",
                    Value = JsonNode.Parse("""
                        {
                          "bookId": "11111111-0000-0000-0000-000000000001",
                          "quantity": 10,
                          "customerId": "cus_s3_bulk",
                          "payment": { "type": "CREDIT_CARD", "fingerprint": "fp_s3_bulk_demo", "last4": "5678" }
                        }
                        """)
                }
            };
            return Task.CompletedTask;
        });
}
