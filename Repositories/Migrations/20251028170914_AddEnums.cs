using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop check constraints that reference the columns before altering types
            migrationBuilder.Sql(@"IF OBJECT_ID('CK_Violations_Severity','C') IS NOT NULL ALTER TABLE [Violations] DROP CONSTRAINT [CK_Violations_Severity];");
            migrationBuilder.Sql(@"IF OBJECT_ID('CK_Submissions_Status','C') IS NOT NULL ALTER TABLE [Submissions] DROP CONSTRAINT [CK_Submissions_Status];");
            migrationBuilder.Sql(@"IF OBJECT_ID('CK_Users_Role','C') IS NOT NULL ALTER TABLE [Users] DROP CONSTRAINT [CK_Users_Role];");

            migrationBuilder.AlterColumn<int>(
                name: "ViolationType",
                table: "Violations",
                type: "int",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "Severity",
                table: "Violations",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Warning");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            // Recreate check constraints with enum textual values no longer applicable; use int ranges if needed
            migrationBuilder.Sql(@"ALTER TABLE [Violations] ADD CONSTRAINT [CK_Violations_Severity] CHECK ([Severity] IN (1,2));");
            migrationBuilder.Sql(@"ALTER TABLE [Submissions] ADD CONSTRAINT [CK_Submissions_Status] CHECK ([Status] IN (1,2,3,4));");
            migrationBuilder.Sql(@"ALTER TABLE [Users] ADD CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (1,2,3,4));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ViolationType",
                table: "Violations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "Violations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Warning",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Submissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);
        }
    }
}
