using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreService.Migrations
{
    /// <inheritdoc />
    public partial class AddRubricScoreEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RubricScore",
                schema: "Core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RubricItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GradedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricScore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricScore_RubricItem_RubricItemId",
                        column: x => x.RubricItemId,
                        principalSchema: "Core",
                        principalTable: "RubricItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RubricScore_RubricItemId",
                schema: "Core",
                table: "RubricScore",
                column: "RubricItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricScore_SubmissionId_RubricItemId_GradedBy",
                schema: "Core",
                table: "RubricScore",
                columns: new[] { "SubmissionId", "RubricItemId", "GradedBy" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RubricScore",
                schema: "Core");
        }
    }
}
