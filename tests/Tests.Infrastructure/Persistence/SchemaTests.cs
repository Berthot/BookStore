using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tests.Shared.Attributes;

namespace Tests.Infrastructure.Persistence;

[Integration]
public sealed class SchemaTests
{
    private NpgsqlConnection _connection = null!;

    [SetUp]
    public async Task SetUpAsync()
    {
        if (string.IsNullOrEmpty(PostgresContainerFixture.ConnectionString))
            Assert.Ignore("Docker not available — skipping integration test");

        _connection = new NpgsqlConnection(PostgresContainerFixture.ConnectionString);
        await _connection.OpenAsync();
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task BookStore_schema_exists()
    {
        var exists = await SchemaExistsAsync("bookstore");
        exists.Should().BeTrue("schema 'bookstore' must be created by migrations");
    }

    [Test]
    public async Task Fraud_schema_exists()
    {
        var exists = await SchemaExistsAsync("fraud");
        exists.Should().BeTrue("schema 'fraud' must be created by migrations");
    }

    [Test]
    public async Task Books_table_is_in_bookstore_schema()
    {
        var exists = await TableExistsAsync("bookstore", "books");
        exists.Should().BeTrue("Table 'books' must be in schema 'bookstore'");
    }

    [Test]
    public async Task Purchases_table_is_in_bookstore_schema()
    {
        var exists = await TableExistsAsync("bookstore", "purchases");
        exists.Should().BeTrue("Table 'purchases' must be in schema 'bookstore'");
    }

    [Test]
    public async Task IdempotencyKeys_table_is_in_bookstore_schema()
    {
        var exists = await TableExistsAsync("bookstore", "idempotency_keys");
        exists.Should().BeTrue("Idempotency keys table must be in schema 'bookstore'");
    }

    [Test]
    public async Task Transactions_table_is_in_fraud_schema()
    {
        var exists = await TableExistsAsync("fraud", "transactions");
        exists.Should().BeTrue("Table 'transactions' must be in schema 'fraud'");
    }

    [Test]
    public async Task Assessments_table_is_in_fraud_schema()
    {
        var exists = await TableExistsAsync("fraud", "assessments");
        exists.Should().BeTrue("Table 'assessments' must be in schema 'fraud'");
    }

    [Test]
    public async Task IdempotencyKeys_table_is_in_fraud_schema()
    {
        var exists = await TableExistsAsync("fraud", "idempotency_keys");
        exists.Should().BeTrue("Idempotency keys table must be in schema 'fraud'");
    }

    [Test]
    public async Task Books_table_is_NOT_in_fraud_schema()
    {
        var exists = await TableExistsAsync("fraud", "books");
        exists.Should().BeFalse("Table 'books' must NOT exist in schema 'fraud'");
    }

    [Test]
    public async Task Transactions_table_is_NOT_in_bookstore_schema()
    {
        var exists = await TableExistsAsync("bookstore", "transactions");
        exists.Should().BeFalse("Table 'transactions' must NOT exist in schema 'bookstore'");
    }

    [Test]
    public async Task BookStore_migrations_history_is_in_bookstore_schema()
    {
        var exists = await TableExistsAsync("bookstore", "__EFMigrationsHistory");
        exists.Should().BeTrue("EF migrations history must be in schema 'bookstore'");
    }

    [Test]
    public async Task Fraud_migrations_history_is_in_fraud_schema()
    {
        var exists = await TableExistsAsync("fraud", "__EFMigrationsHistory");
        exists.Should().BeTrue("EF migrations history must be in schema 'fraud'");
    }

    private async Task<bool> SchemaExistsAsync(string schema)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS(SELECT 1 FROM information_schema.schemata WHERE schema_name = @schema)",
            _connection);
        cmd.Parameters.AddWithValue("schema", schema);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<bool> TableExistsAsync(string schema, string table)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)",
            _connection);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }
}
