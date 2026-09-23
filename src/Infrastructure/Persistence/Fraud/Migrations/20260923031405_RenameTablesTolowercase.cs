using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Fraud.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesTolowercase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assessments_Transactions_TransactionId",
                schema: "fraud",
                table: "Assessments");

            migrationBuilder.DropTable(
                name: "RuleEvaluations",
                schema: "fraud");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                schema: "fraud",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Assessments",
                schema: "fraud",
                table: "Assessments");

            migrationBuilder.RenameTable(
                name: "Transactions",
                schema: "fraud",
                newName: "transactions",
                newSchema: "fraud");

            migrationBuilder.RenameTable(
                name: "Assessments",
                schema: "fraud",
                newName: "assessments",
                newSchema: "fraud");

            migrationBuilder.RenameIndex(
                name: "IX_Assessments_TransactionId",
                schema: "fraud",
                table: "assessments",
                newName: "IX_assessments_TransactionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transactions",
                schema: "fraud",
                table: "transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_assessments",
                schema: "fraud",
                table: "assessments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "rule_evaluations",
                schema: "fraud",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RuleVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Hit = table.Column<bool>(type: "boolean", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rule_evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rule_evaluations_assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalSchema: "fraud",
                        principalTable: "assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rule_evaluations_AssessmentId",
                schema: "fraud",
                table: "rule_evaluations",
                column: "AssessmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_assessments_transactions_TransactionId",
                schema: "fraud",
                table: "assessments",
                column: "TransactionId",
                principalSchema: "fraud",
                principalTable: "transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assessments_transactions_TransactionId",
                schema: "fraud",
                table: "assessments");

            migrationBuilder.DropTable(
                name: "rule_evaluations",
                schema: "fraud");

            migrationBuilder.DropPrimaryKey(
                name: "PK_transactions",
                schema: "fraud",
                table: "transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_assessments",
                schema: "fraud",
                table: "assessments");

            migrationBuilder.RenameTable(
                name: "transactions",
                schema: "fraud",
                newName: "Transactions",
                newSchema: "fraud");

            migrationBuilder.RenameTable(
                name: "assessments",
                schema: "fraud",
                newName: "Assessments",
                newSchema: "fraud");

            migrationBuilder.RenameIndex(
                name: "IX_assessments_TransactionId",
                schema: "fraud",
                table: "Assessments",
                newName: "IX_Assessments_TransactionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                schema: "fraud",
                table: "Transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Assessments",
                schema: "fraud",
                table: "Assessments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "RuleEvaluations",
                schema: "fraud",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hit = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RuleVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(5,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleEvaluations_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalSchema: "fraud",
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleEvaluations_AssessmentId",
                schema: "fraud",
                table: "RuleEvaluations",
                column: "AssessmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assessments_Transactions_TransactionId",
                schema: "fraud",
                table: "Assessments",
                column: "TransactionId",
                principalSchema: "fraud",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
