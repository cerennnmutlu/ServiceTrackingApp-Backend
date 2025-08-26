using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingApi.Models
{
    public class ServiceVehicle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServiceVehicleID { get; set; }

        [Required]
        [StringLength(15)]
        public string PlateNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Brand { get; set; }

        [StringLength(50)]
        public string? Model { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0")]
        public int Capacity { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive

        [ForeignKey("Route")]
        public int RouteID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Route Route { get; set; } = null!;
        public virtual ICollection<VehicleDriverAssignment> VehicleDriverAssignments { get; set; } = new List<VehicleDriverAssignment>();
        public virtual ICollection<VehicleShiftAssignment> VehicleShiftAssignments { get; set; } = new List<VehicleShiftAssignment>();
        public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
    }
}