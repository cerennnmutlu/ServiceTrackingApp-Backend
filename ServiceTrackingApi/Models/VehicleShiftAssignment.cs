using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingApi.Models
{
    public class VehicleShiftAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AssignmentID { get; set; }

        [ForeignKey("ServiceVehicle")]
        public int ServiceVehicleID { get; set; }

        [ForeignKey("Shift")]
        public int ShiftID { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime AssignmentDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ServiceVehicle ServiceVehicle { get; set; } = null!;
        public virtual Shift Shift { get; set; } = null!;
    }
}