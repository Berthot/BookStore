using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.Abstractions.Idempotency;

/// <summary>Computes a deterministic SHA-256 hash over a normalised JSON body so that semantically identical payloads with different whitespace produce the same hash.</summary>
public static class IdempotencyHasher
{
    public static string ComputeHash(string requestBody)
    {
        var normalized = NormalizeJson(requestBody);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizeJson(string body)
    {
        using var doc = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(doc.RootElement);
    }
}
