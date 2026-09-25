using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Fraud.Migrations
{
    /// <inheritdoc />
    public partial class AddXminAndIdempotencyStatusCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin is a PostgreSQL system column — already exists on every table, no DDL needed.
            // EF Core tracks it as a shadow property for optimistic concurrency only.

            migrationBuilder.AddColumn<string>(
                name: "LocationHeader",
                schema: "fraud",
                table: "idempotency_keys",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                schema: "fraud",
                table: "idempotency_keys",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationHeader",
                schema: "fraud",
                table: "idempotency_keys");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                schema: "fraud",
                table: "idempotency_keys");
        }
    }
}
