using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.BookStore.Migrations
{
    /// <inheritdoc />
    public partial class AddFraudOutcomeToPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fraud_outcome",
                schema: "bookstore",
                table: "purchases",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fraud_outcome",
                schema: "bookstore",
                table: "purchases");
        }
    }
}
