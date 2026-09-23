using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Enums;
using Domain.ValueObjects;
using Tests.Shared.Attributes;
using Tests.Shared.Base;

namespace Tests.Application.Serialization;

/// <summary>Verifies that all API-facing enums round-trip as SNAKE_UPPER strings via JsonStringEnumConverter.</summary>
[Unit]
public sealed class EnumSerializationTests : UnitTestsBase
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [TestCase(Outcome.Approved,   "\"APPROVED\"")]
    [TestCase(Outcome.Rejected,   "\"REJECTED\"")]
    [TestCase(Outcome.Review,     "\"REVIEW\"")]
    public void Outcome_serializes_to_snake_upper(Outcome value, string expected)
    {
        var json = JsonSerializer.Serialize(value, Options);
        json.Should().Be(expected);
    }

    [TestCase(TransactionStatus.Received,   "\"RECEIVED\"")]
    [TestCase(TransactionStatus.Processing, "\"PROCESSING\"")]
    [TestCase(TransactionStatus.Decided,    "\"DECIDED\"")]
    public void TransactionStatus_serializes_to_snake_upper(TransactionStatus value, string expected)
    {
        var json = JsonSerializer.Serialize(value, Options);
        json.Should().Be(expected);
    }

    [TestCase(PurchaseStatus.PendingFraudCheck, "\"PENDING_FRAUD_CHECK\"")]
    [TestCase(PurchaseStatus.Confirmed,         "\"CONFIRMED\"")]
    [TestCase(PurchaseStatus.Cancelled,         "\"CANCELLED\"")]
    [TestCase(PurchaseStatus.UnderReview,       "\"UNDER_REVIEW\"")]
    public void PurchaseStatus_serializes_to_snake_upper(PurchaseStatus value, string expected)
    {
        var json = JsonSerializer.Serialize(value, Options);
        json.Should().Be(expected);
    }

    [TestCase(BookFormat.Ebook,     "\"EBOOK\"")]
    [TestCase(BookFormat.Paperback, "\"PAPERBACK\"")]
    [TestCase(BookFormat.Hardcover, "\"HARDCOVER\"")]
    public void BookFormat_serializes_to_snake_upper(BookFormat value, string expected)
    {
        var json = JsonSerializer.Serialize(value, Options);
        json.Should().Be(expected);
    }

    [TestCase(DeciderKind.Engine,   "\"ENGINE\"")]
    [TestCase(DeciderKind.Reviewer, "\"REVIEWER\"")]
    [TestCase(DeciderKind.System,   "\"SYSTEM\"")]
    public void DeciderKind_serializes_to_snake_upper(DeciderKind value, string expected)
    {
        var json = JsonSerializer.Serialize(value, Options);
        json.Should().Be(expected);
    }

    [Test]
    public void Outcome_deserializes_from_snake_upper_string()
    {
        var outcome = JsonSerializer.Deserialize<Outcome>("\"APPROVED\"", Options);
        outcome.Should().Be(Outcome.Approved);
    }
}
