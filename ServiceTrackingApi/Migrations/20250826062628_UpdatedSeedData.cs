using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServiceTrackingApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    DriverID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.DriverID);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    RouteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Distance = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    EstimatedDuration = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.RouteID);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    ShiftID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.ShiftID);
                    table.CheckConstraint("CK_Shift_Times", "[EndTime] <> [StartTime]");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceVehicles",
                columns: table => new
                {
                    ServiceVehicleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlateNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RouteID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceVehicles", x => x.ServiceVehicleID);
                    table.ForeignKey(
                        name: "FK_ServiceVehicles_Routes_RouteID",
                        column: x => x.RouteID,
                        principalTable: "Routes",
                        principalColumn: "RouteID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Trackings",
                columns: table => new
                {
                    TrackingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceVehicleID = table.Column<int>(type: "int", nullable: false),
                    ShiftID = table.Column<int>(type: "int", nullable: false),
                    TrackingDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trackings", x => x.TrackingID);
                    table.CheckConstraint("CK_Tracking_MovementType", "[MovementType] IN ('Entry','Exit')");
                    table.ForeignKey(
                        name: "FK_Trackings_ServiceVehicles_ServiceVehicleID",
                        column: x => x.ServiceVehicleID,
                        principalTable: "ServiceVehicles",
                        principalColumn: "ServiceVehicleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trackings_Shifts_ShiftID",
                        column: x => x.ShiftID,
                        principalTable: "Shifts",
                        principalColumn: "ShiftID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDriverAssignments",
                columns: table => new
                {
                    AssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceVehicleID = table.Column<int>(type: "int", nullable: false),
                    DriverID = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDriverAssignments", x => x.AssignmentID);
                    table.ForeignKey(
                        name: "FK_VehicleDriverAssignments_Drivers_DriverID",
                        column: x => x.DriverID,
                        principalTable: "Drivers",
                        principalColumn: "DriverID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleDriverAssignments_ServiceVehicles_ServiceVehicleID",
                        column: x => x.ServiceVehicleID,
                        principalTable: "ServiceVehicles",
                        principalColumn: "ServiceVehicleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleShiftAssignments",
                columns: table => new
                {
                    AssignmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceVehicleID = table.Column<int>(type: "int", nullable: false),
                    ShiftID = table.Column<int>(type: "int", nullable: false),
                    AssignmentDate = table.Column<DateTime>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleShiftAssignments", x => x.AssignmentID);
                    table.ForeignKey(
                        name: "FK_VehicleShiftAssignments_ServiceVehicles_ServiceVehicleID",
                        column: x => x.ServiceVehicleID,
                        principalTable: "ServiceVehicles",
                        principalColumn: "ServiceVehicleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleShiftAssignments_Shifts_ShiftID",
                        column: x => x.ShiftID,
                        principalTable: "Shifts",
                        principalColumn: "ShiftID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "DriverID", "CreatedAt", "FullName", "Phone", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Ali Yılmaz", "0555 111 22 33", "Active", null },
                    { 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Ayşe Demir", "0555 222 33 44", "Active", null },
                    { 3, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Mehmet Kaya", "0555 333 44 55", "Active", null },
                    { 4, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Fatma Özkan", "0555 444 55 66", "Active", null },
                    { 5, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Ahmet Çelik", "0555 555 66 77", "Inactive", null },
                    { 6, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Zeynep Arslan", "0555 666 77 88", "Active", null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleID", "RoleName" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "ShiftManager" },
                    { 3, "SecurityGuard" }
                });

            migrationBuilder.InsertData(
                table: "Routes",
                columns: new[] { "RouteID", "CreatedAt", "Description", "Distance", "EstimatedDuration", "RouteName", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Merkez hattı", 12.5m, 35, "Merkez-1", "Active", null },
                    { 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "OSB Batı", 22.0m, 55, "Batı Hattı", "Active", null },
                    { 3, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Sanayi Bölgesi Doğu", 18.5m, 45, "Doğu Hattı", "Active", null },
                    { 4, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Üniversite Kampüsü", 15.0m, 40, "Kuzey Hattı", "Active", null },
                    { 5, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Hastane ", 20.0m, 50, "Güney Hattı", "Inactive", null }
                });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "ShiftID", "CreatedAt", "EndTime", "ShiftName", "StartTime", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 18, 30, 0, 0), "Gündüz (08:30-18:30)", new TimeSpan(0, 8, 30, 0, 0), "Active", null },
                    { 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 8, 30, 0, 0), "Gece-1 (00:30-08:30)", new TimeSpan(0, 0, 30, 0, 0), "Active", null },
                    { 3, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 16, 30, 0, 0), "Sabah (08:30-16:30)", new TimeSpan(0, 8, 30, 0, 0), "Active", null },
                    { 4, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 30, 0, 0), "Akşam (16:30-00:30)", new TimeSpan(0, 16, 30, 0, 0), "Active", null },
                    { 5, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 17, 0, 0, 0), "Hafta Sonu (09:00-17:00)", new TimeSpan(0, 9, 0, 0, 0), "Active", null }
                });

            migrationBuilder.InsertData(
                table: "ServiceVehicles",
                columns: new[] { "ServiceVehicleID", "Brand", "Capacity", "CreatedAt", "Model", "PlateNumber", "RouteID", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Mercedes", 16, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Sprinter", "34ABC123", 1, "Active", null },
                    { 2, "Volkswagen", 16, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Crafter", "34XYZ987", 2, "Active", null },
                    { 3, "Ford", 14, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Transit", "06DEF456", 3, "Active", null },
                    { 4, "Mercedes", 18, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Sprinter", "35GHI789", 4, "Active", null },
                    { 5, "Iveco", 12, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Daily", "01JKL012", 1, "Maintenance", null },
                    { 6, "Volkswagen", 16, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Crafter", "16MNO345", 2, "Active", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "CreatedAt", "Email", "FullName", "PasswordHash", "RoleID", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "admin@example.com", "System Admin", "HASHED_PASSWORD_PLACEHOLDER", 1, null, "admin" },
                    { 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "shift@example.com", "Vardiya Müdürü", "HASHED_PASSWORD_PLACEHOLDER", 2, null, "shiftmgr" },
                    { 3, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "security@example.com", "Güvenlik Görevlisi", "HASHED_PASSWORD_PLACEHOLDER", 3, null, "security" }
                });

            migrationBuilder.InsertData(
                table: "Trackings",
                columns: new[] { "TrackingID", "CreatedAt", "MovementType", "ServiceVehicleID", "ShiftID", "TrackingDateTime" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Entry", 1, 1, new DateTimeOffset(new DateTime(2025, 8, 26, 8, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Exit", 1, 1, new DateTimeOffset(new DateTime(2025, 8, 26, 18, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Entry", 2, 1, new DateTimeOffset(new DateTime(2025, 8, 26, 9, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Exit", 2, 1, new DateTimeOffset(new DateTime(2025, 8, 26, 17, 45, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Entry", 3, 3, new DateTimeOffset(new DateTime(2025, 8, 26, 8, 35, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Exit", 3, 3, new DateTimeOffset(new DateTime(2025, 8, 26, 16, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 7, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Entry", 4, 4, new DateTimeOffset(new DateTime(2025, 8, 26, 16, 35, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 8, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Entry", 6, 2, new DateTimeOffset(new DateTime(2025, 8, 27, 0, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "VehicleDriverAssignments",
                columns: new[] { "AssignmentID", "CreatedAt", "DriverID", "EndDate", "ServiceVehicleID", "StartDate" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, 1, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 2, null, 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 3, null, 3, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 4, null, 4, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 6, null, 6, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, new DateTime(2025, 7, 27, 0, 0, 0, 0, DateTimeKind.Utc), 5, new DateTime(2025, 8, 25, 0, 0, 0, 0, DateTimeKind.Utc), 5, new DateTime(2025, 7, 27, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "VehicleShiftAssignments",
                columns: new[] { "AssignmentID", "AssignmentDate", "CreatedAt", "ServiceVehicleID", "ShiftID" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1 },
                    { 2, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 2, 1 },
                    { 3, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 3, 3 },
                    { 4, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 4, 4 },
                    { 5, new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 6, 2 },
                    { 6, new DateTime(2025, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 1, 3 },
                    { 7, new DateTime(2025, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 26, 0, 0, 0, 0, DateTimeKind.Utc), 2, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_RouteName",
                table: "Routes",
                column: "RouteName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceVehicles_PlateNumber",
                table: "ServiceVehicles",
                column: "PlateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceVehicles_RouteID",
                table: "ServiceVehicles",
                column: "RouteID");

            migrationBuilder.CreateIndex(
                name: "IX_Trackings_ServiceVehicleID_ShiftID_TrackingDateTime",
                table: "Trackings",
                columns: new[] { "ServiceVehicleID", "ShiftID", "TrackingDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Trackings_ShiftID",
                table: "Trackings",
                column: "ShiftID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleID",
                table: "Users",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDriverAssignments_DriverID",
                table: "VehicleDriverAssignments",
                column: "DriverID",
                unique: true,
                filter: "[EndDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDriverAssignments_ServiceVehicleID",
                table: "VehicleDriverAssignments",
                column: "ServiceVehicleID",
                unique: true,
                filter: "[EndDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleShiftAssignments_ServiceVehicleID_ShiftID_AssignmentDate",
                table: "VehicleShiftAssignments",
                columns: new[] { "ServiceVehicleID", "ShiftID", "AssignmentDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleShiftAssignments_ShiftID",
                table: "VehicleShiftAssignments",
                column: "ShiftID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trackings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VehicleDriverAssignments");

            migrationBuilder.DropTable(
                name: "VehicleShiftAssignments");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "ServiceVehicles");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Routes");
        }
    }
}
