using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTrackingApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToRealPBKDF2Hashes : Migration
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
            // Eski hash'lere geri dön (önceki migration'daki değerler)
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET PasswordHash = 'PBKDF2-SHA256.10000.salt1234567890123456789012.hash1234567890123456789012345678901234567890123='
                WHERE UserID = 1
            ");

            migrationBuilder.Sql(@"
                UPDATE Users 
                SET PasswordHash = 'PBKDF2-SHA256.10000.salt2345678901234567890123.hash2345678901234567890123456789012345678901234='
                WHERE UserID = 2
            ");

            migrationBuilder.Sql(@"
                UPDATE Users 
                SET PasswordHash = 'PBKDF2-SHA256.10000.salt3456789012345678901234.hash3456789012345678901234567890123456789012345='
                WHERE UserID = 3
            ");
        }
    }
}
