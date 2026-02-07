using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubApplicationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApplicationStatus",
                table: "Clubs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Clubs",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Clubs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedBy",
                table: "Clubs",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$YAj060D1M0jgPcM9HsGx3OEog0aGKnmmWCsq5f7TdnI4r.r2QnZha");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_ApplicationStatus",
                table: "Clubs",
                column: "ApplicationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_ReviewedAt",
                table: "Clubs",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_ReviewedBy",
                table: "Clubs",
                column: "ReviewedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_Users_ReviewedBy",
                table: "Clubs",
                column: "ReviewedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_Users_ReviewedBy",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_ApplicationStatus",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_ReviewedAt",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_ReviewedBy",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ApplicationStatus",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Clubs");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$iMeDtp.b8bCqN9KsHLMS5eh1eIKKA/XuTTA8JlmRyRKKIa712V9NC");
        }
    }
}
