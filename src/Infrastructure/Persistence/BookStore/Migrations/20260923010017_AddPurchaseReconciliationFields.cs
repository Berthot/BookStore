using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.BookStore.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseReconciliationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookFormat",
                schema: "bookstore",
                table: "Purchases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                schema: "bookstore",
                table: "Purchases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentFingerprint",
                schema: "bookstore",
                table: "Purchases",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentLast4",
                schema: "bookstore",
                table: "Purchases",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                schema: "bookstore",
                table: "Purchases",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookFormat",
                schema: "bookstore",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                schema: "bookstore",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaymentFingerprint",
                schema: "bookstore",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaymentLast4",
                schema: "bookstore",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                schema: "bookstore",
                table: "Purchases");
        }
    }
}
