using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace WebApi.OpenApi;

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

    internal static IEndpointConventionBuilder WithTransactionExamples(this IEndpointConventionBuilder builder)
        => builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            if (operation.RequestBody?.Content is not { } content
                || !content.TryGetValue("application/json", out var media))
                return Task.CompletedTask;

            media.Examples = new Dictionary<string, IOpenApiExample>
            {
                ["valid"] = new OpenApiExample
                {
                    Summary = "Transação válida — baixo risco → APPROVED",
                    Value = JsonNode.Parse("""
                        {
                          "externalReference": "ref-demo-001",
                          "customerId": "cus_demo_valid",
                          "amount": { "value": 149.90, "currency": "BRL" },
                          "payment": { "type": "CREDIT_CARD", "fingerprint": "fp_demo_valid", "last4": "1234" },
                          "channel": "WEB",
                          "delivery": "PHYSICAL",
                          "itemCount": 1,
                          "occurredAt": "2026-09-24T12:00:00Z"
                        }
                        """)
                },
                ["s11-structuring-3rd"] = new OpenApiExample
                {
                    Summary = "S11 — 3ª transação (R$490) abaixo do threshold → UNDER_REVIEW (requer 2 anteriores: R$475 e R$480 com mesmo customerId/fingerprint)",
                    Value = JsonNode.Parse("""
                        {
                          "externalReference": "s11-structuring-3",
                          "customerId": "cus_s11_struct_demo",
                          "amount": { "value": 490.00, "currency": "BRL" },
                          "payment": { "type": "CREDIT_CARD", "fingerprint": "fp_s11_struct_demo", "last4": "3333" },
                          "channel": "WEB",
                          "delivery": "PHYSICAL",
                          "itemCount": 1,
                          "occurredAt": "2026-09-24T12:00:00Z"
                        }
                        """)
                }
            };
            return Task.CompletedTask;
        });

    internal static IEndpointConventionBuilder WithReviewExamples(this IEndpointConventionBuilder builder)
        => builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            if (operation.RequestBody?.Content is not { } content
                || !content.TryGetValue("application/json", out var media))
                return Task.CompletedTask;

            media.Examples = new Dictionary<string, IOpenApiExample>
            {
                ["approve"] = new OpenApiExample
                {
                    Summary = "Aprovar — analista confirmou legitimidade da transação",
                    Value = JsonNode.Parse("""
                        {
                          "outcome": "APPROVED",
                          "justification": "Cliente confirmou a compra via telefone. Pedido bulk legítimo para distribuição interna.",
                          "reviewerId": "analyst_01"
                        }
                        """)
                },
                ["reject"] = new OpenApiExample
                {
                    Summary = "Rejeitar — padrão de risco confirmado após investigação manual",
                    Value = JsonNode.Parse("""
                        {
                          "outcome": "REJECTED",
                          "justification": "Padrão de estruturação confirmado. Transações deliberadamente abaixo do limite de reporte.",
                          "reviewerId": "analyst_01"
                        }
                        """)
                }
            };
            return Task.CompletedTask;
        });
}
