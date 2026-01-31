using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$dPZbh4ObXQtVTb2VQAzSE.PpUOf5qPWtetUJ4cagaLKdV2jGBaZfi");

            migrationBuilder.CreateIndex(
                name: "IX_UserMutes_ExpiresAt",
                table: "UserMutes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_ExpiresAt",
                table: "UserBans",
                column: "ExpiresAt");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reports_ExactlyOneTarget",
                table: "Reports",
                sql: "((ReportedUserId IS NOT NULL AND ReportedPostId IS NULL AND ReportedThreadId IS NULL) OR (ReportedUserId IS NULL AND ReportedPostId IS NOT NULL AND ReportedThreadId IS NULL) OR (ReportedUserId IS NULL AND ReportedPostId IS NULL AND ReportedThreadId IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserMutes_ExpiresAt",
                table: "UserMutes");

            migrationBuilder.DropIndex(
                name: "IX_UserBans_ExpiresAt",
                table: "UserBans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reports_ExactlyOneTarget",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$l5Hg2BVQy.dVXR5e9//MMejReJBEIZjQB4Wn9Ik.xI7Q4biM1Upfu");
        }
    }
}
