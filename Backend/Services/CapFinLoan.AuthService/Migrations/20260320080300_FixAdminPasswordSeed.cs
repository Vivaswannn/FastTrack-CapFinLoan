using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapFinLoan.AuthService.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminPasswordSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "auth",
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2a$11$sSgwsvIunYMs0wYal/0y6.09HHvD1GdYn9rPjH7OJDNPzTJQJDMgm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "auth",
                table: "Users",
                keyColumn: "UserId",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2a$11$8K1p/a0dqbQILYKMBCQSYOX7OJaJ3bqEGJgGBx5QmKXRZKvVqFZ0m");
        }
    }
}
