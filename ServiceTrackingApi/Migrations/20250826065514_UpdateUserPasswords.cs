using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTrackingApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "Email", "PasswordHash" },
                values: new object[] { "test@admin.com", "123123" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                columns: new[] { "Email", "PasswordHash" },
                values: new object[] { "test@shift.com", "123123" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 3,
                columns: new[] { "Email", "PasswordHash" },
                values: new object[] { "test@security.com", "123123" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                columns: new[] { "Email", "PasswordHash" },
                values: new object[] { "admin@example.com", "HASHED_PASSWORD_PLACEHOLDER" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                columns: new[] { "Email", "PasswordHash" },
                values: new object[] { "shift@example.com", "HASHED_PASSWORD_PLACEHOLDER" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 3,
                columns: new[] { "Email", "PasswordHash" },
                values: new object[] { "security@example.com", "HASHED_PASSWORD_PLACEHOLDER" });
        }
    }
}
