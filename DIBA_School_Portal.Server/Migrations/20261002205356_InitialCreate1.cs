using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_School_Portal.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TermResults_SchoolClass_ClassId1",
                table: "TermResults");

            migrationBuilder.DropIndex(
                name: "IX_TermResults_ClassId1",
                table: "TermResults");

            migrationBuilder.DropColumn(
                name: "ClassId1",
                table: "TermResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClassId1",
                table: "TermResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TermResults_ClassId1",
                table: "TermResults",
                column: "ClassId1");

            migrationBuilder.AddForeignKey(
                name: "FK_TermResults_SchoolClass_ClassId1",
                table: "TermResults",
                column: "ClassId1",
                principalTable: "SchoolClass",
                principalColumn: "Id");
        }
    }
}
