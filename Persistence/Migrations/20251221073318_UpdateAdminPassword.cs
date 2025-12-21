using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Admin kullanıcısının şifresini güncelle
            // Yeni hash "admin" şifresi için BCrypt ile oluşturuldu
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$VQ2E3YfZvKMN7YH8dKGXUeHvGr/BDYlLcjHLqKz5F3Qz8NZ3YYzHm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eski hash'e geri dön
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$hKvNNt0fHLsqvVVdKp8R9e2DfWQG3VqLz0rXG0.CqE.8FGvN.3rXe");
        }
    }
}
