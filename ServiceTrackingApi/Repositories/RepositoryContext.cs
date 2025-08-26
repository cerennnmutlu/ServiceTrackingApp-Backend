// RepositoryContex.cs
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Models;
using RouteModel = ServiceTrackingApi.Models.Route; // Route adı çakışmasın

namespace ServiceTrackingApi.Data
{
    public class RepositoryContext : DbContext
    {
        public RepositoryContext(DbContextOptions<RepositoryContext> options) : base(options) { }

        // DbSets
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<RouteModel> Routes => Set<RouteModel>();
        public DbSet<ServiceVehicle> ServiceVehicles => Set<ServiceVehicle>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<VehicleShiftAssignment> VehicleShiftAssignments => Set<VehicleShiftAssignment>();
        public DbSet<VehicleDriverAssignment> VehicleDriverAssignments => Set<VehicleDriverAssignment>();
        public DbSet<Tracking> Trackings => Set<Tracking>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ---------- Role ----------
            b.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            // ---------- User ----------
            b.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            b.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            b.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Driver ----------
            b.Entity<Driver>()
                .Property(d => d.Status)
                .HasMaxLength(20);

            // ---------- Route ----------
            b.Entity<RouteModel>()
                .HasIndex(r => r.RouteName)
                .IsUnique(); // Her rota adı tekil olsun (istersen kaldır)

            // ---------- ServiceVehicle ----------
            b.Entity<ServiceVehicle>()
                .HasIndex(v => v.PlateNumber)
                .IsUnique();

            b.Entity<ServiceVehicle>()
                .HasOne(v => v.Route)
                .WithMany(r => r.ServiceVehicles)
                .HasForeignKey(v => v.RouteID)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Shift ----------
            // Gece devrine izin: sadece aynı saat olmasın
            b.Entity<Shift>()
                .ToTable(t => t.HasCheckConstraint("CK_Shift_Times", "[EndTime] <> [StartTime]"));

            // ---------- VehicleShiftAssignment ----------
            b.Entity<VehicleShiftAssignment>()
                .Property(p => p.AssignmentDate)
                .HasColumnType("date");

            // Aynı gün - aynı araç - aynı vardiya tekil
            b.Entity<VehicleShiftAssignment>()
                .HasIndex(x => new { x.ServiceVehicleID, x.ShiftID, x.AssignmentDate })
                .IsUnique();

            b.Entity<VehicleShiftAssignment>()
                .HasOne(a => a.ServiceVehicle)
                .WithMany(v => v.VehicleShiftAssignments)
                .HasForeignKey(a => a.ServiceVehicleID)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<VehicleShiftAssignment>()
                .HasOne(a => a.Shift)
                .WithMany(s => s.VehicleShiftAssignments)
                .HasForeignKey(a => a.ShiftID)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------- VehicleDriverAssignment ----------
            // Tek aktif atama: EndDate NULL olan kayıtlar tekil (hem araç hem sürücü bazında)
            b.Entity<VehicleDriverAssignment>()
                .HasIndex(x => x.ServiceVehicleID)
                .HasFilter("[EndDate] IS NULL")
                .IsUnique();

            b.Entity<VehicleDriverAssignment>()
                .HasIndex(x => x.DriverID)
                .HasFilter("[EndDate] IS NULL")
                .IsUnique();

            b.Entity<VehicleDriverAssignment>()
                .HasOne(a => a.ServiceVehicle)
                .WithMany(v => v.VehicleDriverAssignments)
                .HasForeignKey(a => a.ServiceVehicleID)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<VehicleDriverAssignment>()
                .HasOne(a => a.Driver)
                .WithMany(d => d.VehicleDriverAssignments)
                .HasForeignKey(a => a.DriverID)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------- Tracking ----------
            b.Entity<Tracking>()
                .HasIndex(t => new { t.ServiceVehicleID, t.ShiftID, t.TrackingDateTime });

            // Entry/Exit kontrolü (opsiyonel): iki değerle sınırlamak için CHECK
            b.Entity<Tracking>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Tracking_MovementType",
                    "[MovementType] IN ('Entry','Exit')"));

            b.Entity<Tracking>()
                .HasOne(t => t.ServiceVehicle)
                .WithMany(v => v.Trackings)
                .HasForeignKey(t => t.ServiceVehicleID)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Tracking>()
                .HasOne(t => t.Shift)
                .WithMany(s => s.Trackings)
                .HasForeignKey(t => t.ShiftID)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- SEED DATA ----------
            // Sabit tarihleri kullan (EF HasData gereği sabit olmalı)
            var seedDate = new DateTime(2025, 08, 26, 0, 0, 0, DateTimeKind.Utc);
            var tz0 = new DateTimeOffset(2025, 08, 26, 08, 30, 00, TimeSpan.Zero);
            var tz1 = new DateTimeOffset(2025, 08, 26, 18, 30, 00, TimeSpan.Zero);
            var tz2 = new DateTimeOffset(2025, 08, 26, 09, 15, 00, TimeSpan.Zero);
            var tz3 = new DateTimeOffset(2025, 08, 26, 17, 45, 00, TimeSpan.Zero);
            var tz4 = new DateTimeOffset(2025, 08, 26, 16, 30, 00, TimeSpan.Zero);
            var tz5 = new DateTimeOffset(2025, 08, 27, 00, 30, 00, TimeSpan.Zero);

            // Roles
            b.Entity<Role>().HasData(
                new Role { RoleID = 1, RoleName = "Admin" },
                new Role { RoleID = 2, RoleName = "ShiftManager" },
                new Role { RoleID = 3, RoleName = "SecurityGuard" }
            );

            // Users (şifre: admin123, shift123, security123)
            b.Entity<User>().HasData(
                new User
                {
                    UserID = 1,
                    FullName = "System Admin",
                    Username = "admin",
                    Email = "test@admin.com",
                    PasswordHash = "GR7mrJGQez9rgBazmSXGlokm4E0PnGHUDaf1aN1q5uc=", // admin123 + salt (Base64)
                    RoleID = 1,
                    CreatedAt = seedDate
                },
                new User
                {
                    UserID = 2,
                    FullName = "Vardiya Müdürü",
                    Username = "shiftmgr",
                    Email = "test@shift.com",
                    PasswordHash = "cnQ4k7c2jxbnFpqyqyv1bS1lWgZThX3OwT8aiNuYuYM=", // shift123 + salt (Base64)
                    RoleID = 2,
                    CreatedAt = seedDate
                },
                new User
                {
                    UserID = 3,
                    FullName = "Güvenlik Görevlisi",
                    Username = "security",
                    Email = "test@security.com",
                    PasswordHash = "LbtnT+rccw9WaeDo69D+0eblN7lCWfhEsWlU1XXdZKI=", // security123 + salt (Base64)
                    RoleID = 3,
                    CreatedAt = seedDate
                }
            );

            // Drivers
            b.Entity<Driver>().HasData(
                new Driver { DriverID = 1, FullName = "Ali Yılmaz", Phone = "0555 111 22 33", Status = "Active", CreatedAt = seedDate },
                new Driver { DriverID = 2, FullName = "Ayşe Demir", Phone = "0555 222 33 44", Status = "Active", CreatedAt = seedDate },
                new Driver { DriverID = 3, FullName = "Mehmet Kaya", Phone = "0555 333 44 55", Status = "Active", CreatedAt = seedDate },
                new Driver { DriverID = 4, FullName = "Fatma Özkan", Phone = "0555 444 55 66", Status = "Active", CreatedAt = seedDate },
                new Driver { DriverID = 5, FullName = "Ahmet Çelik", Phone = "0555 555 66 77", Status = "Inactive", CreatedAt = seedDate },
                new Driver { DriverID = 6, FullName = "Zeynep Arslan", Phone = "0555 666 77 88", Status = "Active", CreatedAt = seedDate }
            );

            // Routes
            b.Entity<RouteModel>().HasData(
                new RouteModel { RouteID = 1, RouteName = "Merkez-1", Description = "Merkez hattı", Distance = 12.5m, EstimatedDuration = 35, Status = "Active", CreatedAt = seedDate },
                new RouteModel { RouteID = 2, RouteName = "Batı Hattı", Description = "OSB Batı", Distance = 22.0m, EstimatedDuration = 55, Status = "Active", CreatedAt = seedDate },
                new RouteModel { RouteID = 3, RouteName = "Doğu Hattı", Description = "Sanayi Bölgesi Doğu", Distance = 18.5m, EstimatedDuration = 45, Status = "Active", CreatedAt = seedDate },
                new RouteModel { RouteID = 4, RouteName = "Kuzey Hattı", Description = "Üniversite Kampüsü", Distance = 15.0m, EstimatedDuration = 40, Status = "Active", CreatedAt = seedDate },
                new RouteModel { RouteID = 5, RouteName = "Güney Hattı", Description = "Hastane ", Distance = 20.0m, EstimatedDuration = 50, Status = "Inactive", CreatedAt = seedDate }
            );

            // Shifts (5 vardiya örneği)
            b.Entity<Shift>().HasData(
                new Shift { ShiftID = 1, ShiftName = "Gündüz (08:30-18:30)", StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(18, 30, 0), Status = "Active", CreatedAt = seedDate },
                new Shift { ShiftID = 2, ShiftName = "Gece-1 (00:30-08:30)",   StartTime = new TimeSpan(0, 30, 0), EndTime = new TimeSpan(8, 30, 0),  Status = "Active", CreatedAt = seedDate },
                new Shift { ShiftID = 3, ShiftName = "Sabah (08:30-16:30)",    StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(16, 30, 0), Status = "Active", CreatedAt = seedDate },
                new Shift { ShiftID = 4, ShiftName = "Akşam (16:30-00:30)",    StartTime = new TimeSpan(16, 30, 0),EndTime = new TimeSpan(0, 30, 0),  Status = "Active", CreatedAt = seedDate },
                new Shift { ShiftID = 5, ShiftName = "Hafta Sonu (09:00-17:00)", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), Status = "Active", CreatedAt = seedDate }
            );

            // Vehicles
            b.Entity<ServiceVehicle>().HasData(
                new ServiceVehicle { ServiceVehicleID = 1, PlateNumber = "34ABC123", Brand = "Mercedes", Model = "Sprinter", Capacity = 16, Status = "Active", RouteID = 1, CreatedAt = seedDate },
                new ServiceVehicle { ServiceVehicleID = 2, PlateNumber = "34XYZ987", Brand = "Volkswagen", Model = "Crafter",  Capacity = 16, Status = "Active", RouteID = 2, CreatedAt = seedDate },
                new ServiceVehicle { ServiceVehicleID = 3, PlateNumber = "06DEF456", Brand = "Ford", Model = "Transit", Capacity = 14, Status = "Active", RouteID = 3, CreatedAt = seedDate },
                new ServiceVehicle { ServiceVehicleID = 4, PlateNumber = "35GHI789", Brand = "Mercedes", Model = "Sprinter", Capacity = 18, Status = "Active", RouteID = 4, CreatedAt = seedDate },
                new ServiceVehicle { ServiceVehicleID = 5, PlateNumber = "01JKL012", Brand = "Iveco", Model = "Daily", Capacity = 12, Status = "Maintenance", RouteID = 1, CreatedAt = seedDate },
                new ServiceVehicle { ServiceVehicleID = 6, PlateNumber = "16MNO345", Brand = "Volkswagen", Model = "Crafter", Capacity = 16, Status = "Active", RouteID = 2, CreatedAt = seedDate }
            );

            // Vehicle-Driver Assignments (aktif)
            b.Entity<VehicleDriverAssignment>().HasData(
                new VehicleDriverAssignment { AssignmentID = 1, ServiceVehicleID = 1, DriverID = 1, StartDate = seedDate, EndDate = null, CreatedAt = seedDate },
                new VehicleDriverAssignment { AssignmentID = 2, ServiceVehicleID = 2, DriverID = 2, StartDate = seedDate, EndDate = null, CreatedAt = seedDate },
                new VehicleDriverAssignment { AssignmentID = 3, ServiceVehicleID = 3, DriverID = 3, StartDate = seedDate, EndDate = null, CreatedAt = seedDate },
                new VehicleDriverAssignment { AssignmentID = 4, ServiceVehicleID = 4, DriverID = 4, StartDate = seedDate, EndDate = null, CreatedAt = seedDate },
                new VehicleDriverAssignment { AssignmentID = 5, ServiceVehicleID = 6, DriverID = 6, StartDate = seedDate, EndDate = null, CreatedAt = seedDate },
                // Geçmiş atama örneği
                new VehicleDriverAssignment { AssignmentID = 6, ServiceVehicleID = 5, DriverID = 5, StartDate = seedDate.AddDays(-30), EndDate = seedDate.AddDays(-1), CreatedAt = seedDate.AddDays(-30) }
            );

            // Vehicle-Shift Assignments (bugün için)
            b.Entity<VehicleShiftAssignment>().HasData(
                new VehicleShiftAssignment { AssignmentID = 1, ServiceVehicleID = 1, ShiftID = 1, AssignmentDate = new DateTime(2025, 08, 26), CreatedAt = seedDate },
                new VehicleShiftAssignment { AssignmentID = 2, ServiceVehicleID = 2, ShiftID = 1, AssignmentDate = new DateTime(2025, 08, 26), CreatedAt = seedDate },
                new VehicleShiftAssignment { AssignmentID = 3, ServiceVehicleID = 3, ShiftID = 3, AssignmentDate = new DateTime(2025, 08, 26), CreatedAt = seedDate },
                new VehicleShiftAssignment { AssignmentID = 4, ServiceVehicleID = 4, ShiftID = 4, AssignmentDate = new DateTime(2025, 08, 26), CreatedAt = seedDate },
                new VehicleShiftAssignment { AssignmentID = 5, ServiceVehicleID = 6, ShiftID = 2, AssignmentDate = new DateTime(2025, 08, 26), CreatedAt = seedDate },
                // Yarın için atamalar
                new VehicleShiftAssignment { AssignmentID = 6, ServiceVehicleID = 1, ShiftID = 3, AssignmentDate = new DateTime(2025, 08, 27), CreatedAt = seedDate },
                new VehicleShiftAssignment { AssignmentID = 7, ServiceVehicleID = 2, ShiftID = 4, AssignmentDate = new DateTime(2025, 08, 27), CreatedAt = seedDate }
            );

            // Tracking örnekleri
            b.Entity<Tracking>().HasData(
                // Araç 1 - Gündüz vardiyası
                new Tracking
                {
                    TrackingID = 1,
                    ServiceVehicleID = 1,
                    ShiftID = 1,
                    TrackingDateTime = tz0, // 08:30 Entry
                    MovementType = "Entry",
                    CreatedAt = seedDate
                },
                new Tracking
                {
                    TrackingID = 2,
                    ServiceVehicleID = 1,
                    ShiftID = 1,
                    TrackingDateTime = tz1, // 18:30 Exit
                    MovementType = "Exit",
                    CreatedAt = seedDate
                },
                // Araç 2 - Gündüz vardiyası
                new Tracking
                {
                    TrackingID = 3,
                    ServiceVehicleID = 2,
                    ShiftID = 1,
                    TrackingDateTime = tz2, // 09:15 Entry
                    MovementType = "Entry",
                    CreatedAt = seedDate
                },
                new Tracking
                {
                    TrackingID = 4,
                    ServiceVehicleID = 2,
                    ShiftID = 1,
                    TrackingDateTime = tz3, // 17:45 Exit
                    MovementType = "Exit",
                    CreatedAt = seedDate
                },
                // Araç 3 - Sabah vardiyası
                new Tracking
                {
                    TrackingID = 5,
                    ServiceVehicleID = 3,
                    ShiftID = 3,
                    TrackingDateTime = new DateTimeOffset(2025, 08, 26, 08, 35, 00, TimeSpan.Zero),
                    MovementType = "Entry",
                    CreatedAt = seedDate
                },
                new Tracking
                {
                    TrackingID = 6,
                    ServiceVehicleID = 3,
                    ShiftID = 3,
                    TrackingDateTime = tz4, // 16:30 Exit
                    MovementType = "Exit",
                    CreatedAt = seedDate
                },
                // Araç 4 - Akşam vardiyası
                new Tracking
                {
                    TrackingID = 7,
                    ServiceVehicleID = 4,
                    ShiftID = 4,
                    TrackingDateTime = new DateTimeOffset(2025, 08, 26, 16, 35, 00, TimeSpan.Zero),
                    MovementType = "Entry",
                    CreatedAt = seedDate
                },
                // Araç 6 - Gece vardiyası
                new Tracking
                {
                    TrackingID = 8,
                    ServiceVehicleID = 6,
                    ShiftID = 2,
                    TrackingDateTime = tz5, // 00:30 Entry (ertesi gün)
                    MovementType = "Entry",
                    CreatedAt = seedDate
                }
            );
        }
    }
}
