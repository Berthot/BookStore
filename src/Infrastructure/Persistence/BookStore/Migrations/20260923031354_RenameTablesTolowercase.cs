using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.BookStore.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesTolowercase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Purchases",
                schema: "bookstore",
                table: "Purchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Books",
                schema: "bookstore",
                table: "Books");

            migrationBuilder.RenameTable(
                name: "Purchases",
                schema: "bookstore",
                newName: "purchases",
                newSchema: "bookstore");

            migrationBuilder.RenameTable(
                name: "Books",
                schema: "bookstore",
                newName: "books",
                newSchema: "bookstore");

            migrationBuilder.RenameIndex(
                name: "IX_Purchases_CorrelationId",
                schema: "bookstore",
                table: "purchases",
                newName: "IX_purchases_CorrelationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_purchases",
                schema: "bookstore",
                table: "purchases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_books",
                schema: "bookstore",
                table: "books",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_purchases",
                schema: "bookstore",
                table: "purchases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_books",
                schema: "bookstore",
                table: "books");

            migrationBuilder.RenameTable(
                name: "purchases",
                schema: "bookstore",
                newName: "Purchases",
                newSchema: "bookstore");

            migrationBuilder.RenameTable(
                name: "books",
                schema: "bookstore",
                newName: "Books",
                newSchema: "bookstore");

            migrationBuilder.RenameIndex(
                name: "IX_purchases_CorrelationId",
                schema: "bookstore",
                table: "Purchases",
                newName: "IX_Purchases_CorrelationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Purchases",
                schema: "bookstore",
                table: "Purchases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Books",
                schema: "bookstore",
                table: "Books",
                column: "Id");
        }
    }
}
