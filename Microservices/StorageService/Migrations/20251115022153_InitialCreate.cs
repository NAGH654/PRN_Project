using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Storage");

            migrationBuilder.CreateTable(
                name: "Submission",
                schema: "Storage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalFiles = table.Column<int>(type: "int", nullable: false),
                    TotalSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionFile",
                schema: "Storage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsImage = table.Column<bool>(type: "bit", nullable: false),
                    ImageWidth = table.Column<int>(type: "int", nullable: true),
                    ImageHeight = table.Column<int>(type: "int", nullable: true),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionFile_Submission_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "Storage",
                        principalTable: "Submission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Violation",
                schema: "Storage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Violation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Violation_Submission_SubmissionId",
                        column: x => x.SubmissionId,
                        principalSchema: "Storage",
                        principalTable: "Submission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submission_Status",
                schema: "Storage",
                table: "Submission",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Submission_StudentId_ExamId",
                schema: "Storage",
                table: "Submission",
                columns: new[] { "StudentId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_Submission_SubmittedAt",
                schema: "Storage",
                table: "Submission",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFile_FileHash",
                schema: "Storage",
                table: "SubmissionFile",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFile_SubmissionId",
                schema: "Storage",
                table: "SubmissionFile",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Violation_Severity",
                schema: "Storage",
                table: "Violation",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Violation_SubmissionId_IsResolved",
                schema: "Storage",
                table: "Violation",
                columns: new[] { "SubmissionId", "IsResolved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionFile",
                schema: "Storage");

            migrationBuilder.DropTable(
                name: "Violation",
                schema: "Storage");

            migrationBuilder.DropTable(
                name: "Submission",
                schema: "Storage");
        }
    }
}
