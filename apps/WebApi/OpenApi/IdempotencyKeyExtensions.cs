using Microsoft.OpenApi;

namespace WebApi.OpenApi;

public static class IdempotencyKeyExtensions
{
    /// <summary>Declares Idempotency-Key as a required request header in the OpenAPI operation for this endpoint.</summary>
    public static TBuilder RequireIdempotencyKey<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
#pragma warning disable ASPDEPR002
        builder.WithOpenApi(op =>
        {
            (op.Parameters ??= []).Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Client-generated unique key (UUID v4 recommended). Repeated requests with the same key and body replay the original response; a different body returns 422.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" }
            });
            return op;
        });
#pragma warning restore ASPDEPR002
}
