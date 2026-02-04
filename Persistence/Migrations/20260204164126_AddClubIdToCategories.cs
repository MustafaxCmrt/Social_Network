using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubIdToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "Categories",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$iMeDtp.b8bCqN9KsHLMS5eh1eIKKA/XuTTA8JlmRyRKKIa712V9NC");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ClubId",
                table: "Categories",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ClubId_IsDeleted",
                table: "Categories",
                columns: new[] { "ClubId", "IsDeleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Clubs_ClubId",
                table: "Categories",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Clubs_ClubId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ClubId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ClubId_IsDeleted",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Categories");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$dPZbh4ObXQtVTb2VQAzSE.PpUOf5qPWtetUJ4cagaLKdV2jGBaZfi");
        }
    }
}
