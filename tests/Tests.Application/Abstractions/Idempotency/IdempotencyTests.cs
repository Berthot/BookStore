using Application.Abstractions.Idempotency;
using Tests.Shared.Attributes;
using Tests.Shared.Constants;

namespace Tests.Application.Abstractions.Idempotency;

[Unit]
public sealed class IdempotencyHasherTests
{
    [Test]
    public void ComputeHash_SameJsonDifferentWhitespace_ProducesSameHash()
    {
        var compact = IdempotencyHasher.ComputeHash("{\"name\":\"Alice\",\"age\":30}");
        var spaced = IdempotencyHasher.ComputeHash("{ \"name\" : \"Alice\" , \"age\" : 30 }");

        compact.Should().Be(spaced);
    }

    [Test]
    public void ComputeHash_SameJsonWithAndWithoutIndent_ProducesSameHash()
    {
        var oneLine = IdempotencyHasher.ComputeHash("{\"x\":1,\"y\":2}");
        var indented = IdempotencyHasher.ComputeHash("{\n  \"x\": 1,\n  \"y\": 2\n}");

        oneLine.Should().Be(indented);
    }

    [Test]
    public void ComputeHash_DifferentBodies_ProduceDifferentHashes()
    {
        var hash1 = IdempotencyHasher.ComputeHash("{\"amount\":100}");
        var hash2 = IdempotencyHasher.ComputeHash("{\"amount\":200}");

        hash1.Should().NotBe(hash2);
    }

    [Test]
    public void ComputeHash_Result_IsSha256HexString_Of64Chars()
    {
        var hash = IdempotencyHasher.ComputeHash("{\"id\":\"abc\"}");

        hash.Length.Should().Be(IdempotencyEntry.HashMaxLength);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }
}

[Unit]
public sealed class IdempotencyEntryTests
{
    [Test]
    public void Create_SetsKeyHashAndProcessingStatus()
    {
        var now = TestConstants.Dates.FixedUtcNow;
        var entry = IdempotencyEntry.Create("key-001", "abc123", now);

        entry.Key.Should().Be("key-001");
        entry.BodyHash.Should().Be("abc123");
        entry.Status.Should().Be(IdempotencyStatus.Processing);
        entry.ResponseBody.Should().BeNull();
        entry.ResourceId.Should().BeNull();
        entry.CreatedAt.Should().Be(now);
    }

    [Test]
    public void Complete_TransitionsToCompletedAndStoresResponse()
    {
        var entry = IdempotencyEntry.Create("key-002", "hash-xyz", TestConstants.Dates.FixedUtcNow);
        var resourceId = Guid.NewGuid();

        entry.Complete("{\"id\":\"r1\"}", resourceId);

        entry.Status.Should().Be(IdempotencyStatus.Completed);
        entry.ResponseBody.Should().Be("{\"id\":\"r1\"}");
        entry.ResourceId.Should().Be(resourceId);
    }

    [Test]
    public void Complete_WithoutResourceId_LeavesResourceIdNull()
    {
        var entry = IdempotencyEntry.Create("key-003", "hash", TestConstants.Dates.FixedUtcNow);

        entry.Complete("{\"ok\":true}");

        entry.Status.Should().Be(IdempotencyStatus.Completed);
        entry.ResourceId.Should().BeNull();
    }
}
