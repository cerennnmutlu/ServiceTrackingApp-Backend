using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingApi.Models
{
    public class Shift
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ShiftID { get; set; }

        [Required]
        [StringLength(50)]
        public string ShiftName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Column(TypeName = "time")]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<VehicleShiftAssignment> VehicleShiftAssignments { get; set; } = new List<VehicleShiftAssignment>();
        public virtual ICollection<Tracking> Trackings { get; set; } = new List<Tracking>();
    }
}
