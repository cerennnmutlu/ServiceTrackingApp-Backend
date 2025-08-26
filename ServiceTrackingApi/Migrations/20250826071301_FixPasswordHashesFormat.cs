using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTrackingApi.Migrations
{
    /// <inheritdoc />
    public partial class FixPasswordHashesFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
