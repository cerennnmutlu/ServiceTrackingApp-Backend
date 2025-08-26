using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingApi.Models
{
    public class VehicleDriverAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AssignmentID { get; set; }

        [ForeignKey("ServiceVehicle")]
        public int ServiceVehicleID { get; set; }

        [ForeignKey("Driver")]
        public int DriverID { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime StartDate { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ServiceVehicle ServiceVehicle { get; set; } = null!;
        public virtual Driver Driver { get; set; } = null!;
    }
}