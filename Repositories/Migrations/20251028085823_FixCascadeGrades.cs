using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Submissions_SubmissionId",
                table: "Grades");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Submissions_SubmissionId",
                table: "Grades",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "SubmissionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Submissions_SubmissionId",
                table: "Grades");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Submissions_SubmissionId",
                table: "Grades",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "SubmissionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
