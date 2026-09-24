using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace BookStore.Api.Infrastructure.OpenApi;

public static class IdempotencyKeyExtensions
{
    public static IEndpointConventionBuilder RequireIdempotencyKey(this IEndpointConventionBuilder builder)
        => builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            (operation.Parameters ??= []).Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Client-generated unique key (UUID v4 recommended). Repeated requests with the same key and body replay the original response; a different body returns 422.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
                Example = JsonValue.Create("a1b2c3d4-e5f6-7890-abcd-ef1234567890")
            });
            return Task.CompletedTask;
        });
}
