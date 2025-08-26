using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingApi.Models
{
    public class Tracking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TrackingID { get; set; }

        [ForeignKey("ServiceVehicle")]
        public int ServiceVehicleID { get; set; }

        [ForeignKey("Shift")]
        public int ShiftID { get; set; }

        [Required]
        [Column(TypeName = "datetimeoffset")]
        public DateTimeOffset TrackingDateTime { get; set; }

        [Required]
        [StringLength(10)]
        public string MovementType { get; set; } = string.Empty; // Entry, Exit

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ServiceVehicle ServiceVehicle { get; set; } = null!;
        public virtual Shift Shift { get; set; } = null!;
    }
}