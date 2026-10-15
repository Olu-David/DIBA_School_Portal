using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_School_Portal.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentPaymentItems_Fees_FeeId",
                table: "StudentPaymentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPayments_AcademicSessions_AcademicSessionId",
                table: "StudentPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPayments_SchoolTerms_TermId",
                table: "StudentPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPayments_StudentEnrollments_EnrollmentId",
                table: "StudentPayments");

            migrationBuilder.AlterColumn<Guid>(
                name: "FeeId",
                table: "StudentPaymentItems",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPaymentItems_Fees_FeeId",
                table: "StudentPaymentItems",
                column: "FeeId",
                principalTable: "Fees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPayments_AcademicSessions_AcademicSessionId",
                table: "StudentPayments",
                column: "AcademicSessionId",
                principalTable: "AcademicSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPayments_SchoolTerms_TermId",
                table: "StudentPayments",
                column: "TermId",
                principalTable: "SchoolTerms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPayments_StudentEnrollments_EnrollmentId",
                table: "StudentPayments",
                column: "EnrollmentId",
                principalTable: "StudentEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentPaymentItems_Fees_FeeId",
                table: "StudentPaymentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPayments_AcademicSessions_AcademicSessionId",
                table: "StudentPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPayments_SchoolTerms_TermId",
                table: "StudentPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentPayments_StudentEnrollments_EnrollmentId",
                table: "StudentPayments");

            migrationBuilder.AlterColumn<Guid>(
                name: "FeeId",
                table: "StudentPaymentItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPaymentItems_Fees_FeeId",
                table: "StudentPaymentItems",
                column: "FeeId",
                principalTable: "Fees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPayments_AcademicSessions_AcademicSessionId",
                table: "StudentPayments",
                column: "AcademicSessionId",
                principalTable: "AcademicSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPayments_SchoolTerms_TermId",
                table: "StudentPayments",
                column: "TermId",
                principalTable: "SchoolTerms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentPayments_StudentEnrollments_EnrollmentId",
                table: "StudentPayments",
                column: "EnrollmentId",
                principalTable: "StudentEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
