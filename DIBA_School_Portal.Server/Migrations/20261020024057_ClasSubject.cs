using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_School_Portal.Server.Migrations
{
    /// <inheritdoc />
    public partial class ClasSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassSubjects_SchoolClassId_SubjectId",
                table: "ClassSubjects");

            migrationBuilder.AddColumn<int>(
                name: "Stream",
                table: "ClassSubjects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentGateway = table.Column<int>(type: "integer", nullable: false),
                    Vendor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiptUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RecordedByUserId = table.Column<string>(type: "text", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TermId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ClassSubjects_SchoolClassId_SubjectId",
                table: "ClassSubjects",
                columns: new[] { "SchoolClassId", "SubjectId" },
                unique: true,
                filter: "\"SubjectId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_AcademicSessionId",
                table: "Expenses",
                column: "AcademicSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Category",
                table: "Expenses",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_IsDeleted",
                table: "Expenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_PaymentGateway",
                table: "Expenses",
                column: "PaymentGateway");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ReferenceNumber",
                table: "Expenses",
                column: "ReferenceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropIndex(
                name: "UX_ClassSubjects_SchoolClassId_SubjectId",
                table: "ClassSubjects");

            migrationBuilder.DropColumn(
                name: "Stream",
                table: "ClassSubjects");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjects_SchoolClassId_SubjectId",
                table: "ClassSubjects",
                columns: new[] { "SchoolClassId", "SubjectId" },
                unique: true);
        }
    }
}
