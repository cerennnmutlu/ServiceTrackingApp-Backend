using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingApi.Models
{
    public class Route
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RouteID { get; set; }

        [Required]
        [StringLength(100)]
        public string RouteName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal? Distance { get; set; } // km

        public int? EstimatedDuration { get; set; } // minutes

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<ServiceVehicle> ServiceVehicles { get; set; } = new List<ServiceVehicle>();
    }
}