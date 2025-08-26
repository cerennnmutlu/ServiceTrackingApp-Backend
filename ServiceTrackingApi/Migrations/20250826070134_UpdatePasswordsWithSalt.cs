using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTrackingApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePasswordsWithSalt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                column: "PasswordHash",
                value: "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                column: "PasswordHash",
                value: "VIiMbxxhHm4xwKllklxUHUOhHy4CS9am8UBHEuU2HCk=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 3,
                column: "PasswordHash",
                value: "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 1,
                column: "PasswordHash",
                value: "123123");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 2,
                column: "PasswordHash",
                value: "123123");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserID",
                keyValue: 3,
                column: "PasswordHash",
                value: "123123");
        }
    }
}
