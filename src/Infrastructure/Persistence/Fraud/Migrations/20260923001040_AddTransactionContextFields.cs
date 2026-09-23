using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Fraud.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionContextFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                schema: "fraud",
                table: "Transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                schema: "fraud",
                table: "Transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ItemCount",
                schema: "fraud",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAt",
                schema: "fraud",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "fraud",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExternalReference",
                schema: "fraud",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ItemCount",
                schema: "fraud",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "OccurredAt",
                schema: "fraud",
                table: "Transactions");
        }
    }
}
