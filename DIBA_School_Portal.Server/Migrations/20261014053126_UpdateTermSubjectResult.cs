using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_School_Portal.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTermSubjectResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "TermSubjectResults",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "TermSubjectResults");
        }
    }
}
