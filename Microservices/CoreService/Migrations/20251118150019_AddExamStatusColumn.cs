using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreService.Migrations
{
    /// <inheritdoc />
    public partial class AddExamStatusColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Core",
                table: "Exam",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Core",
                table: "Exam");
        }
    }
}
