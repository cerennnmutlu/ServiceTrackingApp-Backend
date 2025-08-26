using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTrackingApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePasswordsToPBKDF2Format : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Admin kullanıcısı (UserID = 1) - admin123
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET PasswordHash = 'PBKDF2-SHA256.100000.esySvi48IC9Xe4EBPIhvpw==.p8wNO7Y8Esk0gLGGFbp5hXFwctZYsu4ol+RVCCWqhis='
                WHERE UserID = 1
            ");

            // Shift kullanıcısı (UserID = 2) - shift123
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET PasswordHash = 'PBKDF2-SHA256.100000.cJfBxvv2/OtLfBW5KONZPw==.9M0bC90M2MKS8aJIQegXgX/m5ugrwg9jGbU25KVGCKo='
                WHERE UserID = 2
            ");

            // Security kullanıcısı (UserID = 3) - security123
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET PasswordHash = 'PBKDF2-SHA256.100000.2YUmOzZJ9mqKmykDMIwf2g==.Jm1ZKiE6mbApgMHU27Yyo6mz1jCgTgcLeOi2byDX2F0='
                WHERE UserID = 3
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eski hash'lere geri dön
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                column: "PasswordHash",
                value: "GR7mrJGQez9rgBazmSXGlokm4E0PnGHUDaf1aN1q5uc=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                column: "PasswordHash",
                value: "cnQ4k7c2jxbnFpqyqyv1bS1lWgZThX3OwT8aiNuYuYM=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 3,
                column: "PasswordHash",
                value: "LbtnT+rccw9WaeDo69D+0eblN7lCWfhEsWlU1XXdZKI=");
        }
    }
}
