using Application.Abstractions.Idempotency;
using Application.UseCases.Idempotency.PurgeExpiredKeys;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Constants;

namespace Tests.Application.UseCases.Idempotency;

[Unit]
public sealed class PurgeExpiredKeysHandlerTests : UnitTestsBase
{
    private IBookStoreIdempotencyStore _bookStoreStore = null!;
    private IFraudIdempotencyStore _fraudStore = null!;
    private PurgeExpiredKeysHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _bookStoreStore = Substitute.For<IBookStoreIdempotencyStore>();
        _fraudStore = Substitute.For<IFraudIdempotencyStore>();
        _handler = new PurgeExpiredKeysHandler(_bookStoreStore, _fraudStore);
    }

    [Test]
    public async Task Handle_deletes_from_both_stores_with_given_threshold()
    {
        var before = TestConstants.Dates.FixedUtcNow;
        _bookStoreStore.DeleteExpiredAsync(before, Arg.Any<CancellationToken>()).Returns(3);
        _fraudStore.DeleteExpiredAsync(before, Arg.Any<CancellationToken>()).Returns(2);

        var result = await _handler.Handle(new PurgeExpiredKeysRequest(before), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalDeleted.Should().Be(5);
    }

    [Test]
    public async Task Handle_calls_delete_on_bookstore_store()
    {
        var before = TestConstants.Dates.FixedUtcNow;
        _bookStoreStore.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        _fraudStore.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);

        await _handler.Handle(new PurgeExpiredKeysRequest(before), CancellationToken.None);

        await _bookStoreStore.Received(1).DeleteExpiredAsync(before, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_calls_delete_on_fraud_store()
    {
        var before = TestConstants.Dates.FixedUtcNow;
        _bookStoreStore.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        _fraudStore.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);

        await _handler.Handle(new PurgeExpiredKeysRequest(before), CancellationToken.None);

        await _fraudStore.Received(1).DeleteExpiredAsync(before, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_returns_zero_when_no_expired_keys()
    {
        _bookStoreStore.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        _fraudStore.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);

        var result = await _handler.Handle(
            new PurgeExpiredKeysRequest(TestConstants.Dates.FixedUtcNow), CancellationToken.None);

        result.Data!.TotalDeleted.Should().Be(0);
    }
}
