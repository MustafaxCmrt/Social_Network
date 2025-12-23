using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTrailUserTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedUserId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedUserId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "Threads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedUserId",
                table: "Threads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedUserId",
                table: "Threads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedUserId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedUserId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedUserId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedUserId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedUserId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedUserId", "DeletedUserId", "PasswordHash", "UpdatedUserId" },
                values: new object[] { null, null, "$2a$11$Zx83Kzog670tW4DOImtADeXWfvBYxKq6dYW1D5nqyK3lOlW7GHSMm", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "DeletedUserId",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "UpdatedUserId",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "DeletedUserId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "UpdatedUserId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedUserId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedUserId",
                table: "Categories");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$fyq3EWAFEU.uzzBO4.Y/0uKyYKgmvVLprscafnWFjUfltT3svmQRa");
        }
    }
}
