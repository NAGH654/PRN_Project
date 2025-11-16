using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Identity_Users_Role",
                schema: "Identity",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                schema: "Identity",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 20);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Identity_Users_Role",
                schema: "Identity",
                table: "Users",
                sql: "[Role] IN ('Admin','Manager','Moderator','Examiner')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Identity_Users_Role",
                schema: "Identity",
                table: "Users");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                schema: "Identity",
                table: "Users",
                type: "int",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Identity_Users_Role",
                schema: "Identity",
                table: "Users",
                sql: "[Role] IN ('Admin','Manager','Moderator','Examiner')");
        }
    }
}
