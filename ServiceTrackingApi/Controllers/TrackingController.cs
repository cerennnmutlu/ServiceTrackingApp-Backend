using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Models;
using ServiceTrackingApi.Data;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace ServiceTrackingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TrackingController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public TrackingController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/Tracking
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tracking>>> GetTrackings()
        {
            try
            {
                var trackings = await _context.Trackings
                    .Include(t => t.ServiceVehicle)
                    .Include(t => t.Shift)
                    .OrderByDescending(t => t.TrackingDateTime)
                    .ToListAsync();
                return Ok(trackings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Takip kayıtları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Tracking/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Tracking>> GetTracking(int id)
        {
            try
            {
                var tracking = await _context.Trackings
                    .Include(t => t.ServiceVehicle)
                    .Include(t => t.Shift)
                    .FirstOrDefaultAsync(t => t.TrackingID == id);

                if (tracking == null)
                {
                    return NotFound(new { message = "Takip kaydı bulunamadı." });
                }

                return Ok(tracking);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Takip kaydı getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Tracking/vehicle/5
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<ActionResult<IEnumerable<Tracking>>> GetTrackingsByVehicle(int vehicleId)
        {
            try
            {
                var trackings = await _context.Trackings
                    .Include(t => t.ServiceVehicle)
                    .Include(t => t.Shift)
                    .Where(t => t.ServiceVehicleID == vehicleId)
                    .OrderByDescending(t => t.TrackingDateTime)
                    .ToListAsync();

                return Ok(trackings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç takip kayıtları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Tracking/shift/5
        [HttpGet("shift/{shiftId}")]
        public async Task<ActionResult<IEnumerable<Tracking>>> GetTrackingsByShift(int shiftId)
        {
            try
            {
                var trackings = await _context.Trackings
                    .Include(t => t.ServiceVehicle)
                    .Include(t => t.Shift)
                    .Where(t => t.ShiftID == shiftId)
                    .OrderByDescending(t => t.TrackingDateTime)
                    .ToListAsync();

                return Ok(trackings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Vardiya takip kayıtları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/Tracking
        [HttpPost]
        public async Task<ActionResult<Tracking>> CreateTracking(TrackingCreateDto trackingDto)
        {
            try
            {
                // ServiceVehicle kontrolü
                var vehicle = await _context.ServiceVehicles.FindAsync(trackingDto.ServiceVehicleID);
                if (vehicle == null)
                {
                    return BadRequest(new { message = "Geçersiz servis aracı ID'si." });
                }

                // Shift kontrolü
                var shift = await _context.Shifts.FindAsync(trackingDto.ShiftID);
                if (shift == null)
                {
                    return BadRequest(new { message = "Geçersiz vardiya ID'si." });
                }

                // MovementType kontrolü
                if (trackingDto.MovementType != "Entry" && trackingDto.MovementType != "Exit")
                {
                    return BadRequest(new { message = "Hareket tipi 'Entry' veya 'Exit' olmalıdır." });
                }

                var tracking = new Tracking
                {
                    ServiceVehicleID = trackingDto.ServiceVehicleID,
                    ShiftID = trackingDto.ShiftID,
                    TrackingDateTime = trackingDto.TrackingDateTime,
                    MovementType = trackingDto.MovementType,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Trackings.Add(tracking);
                await _context.SaveChangesAsync();

                // Oluşturulan kaydı ilişkili verilerle birlikte getir
                var createdTracking = await _context.Trackings
                    .Include(t => t.ServiceVehicle)
                    .Include(t => t.Shift)
                    .FirstOrDefaultAsync(t => t.TrackingID == tracking.TrackingID);

                return CreatedAtAction(nameof(GetTracking), new { id = tracking.TrackingID }, createdTracking);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Takip kaydı oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/Tracking/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTracking(int id, TrackingUpdateDto trackingDto)
        {
            try
            {
                var tracking = await _context.Trackings.FindAsync(id);
                if (tracking == null)
                {
                    return NotFound(new { message = "Takip kaydı bulunamadı." });
                }

                // ServiceVehicle kontrolü
                if (trackingDto.ServiceVehicleID.HasValue)
                {
                    var vehicle = await _context.ServiceVehicles.FindAsync(trackingDto.ServiceVehicleID.Value);
                    if (vehicle == null)
                    {
                        return BadRequest(new { message = "Geçersiz servis aracı ID'si." });
                    }
                    tracking.ServiceVehicleID = trackingDto.ServiceVehicleID.Value;
                }

                // Shift kontrolü
                if (trackingDto.ShiftID.HasValue)
                {
                    var shift = await _context.Shifts.FindAsync(trackingDto.ShiftID.Value);
                    if (shift == null)
                    {
                        return BadRequest(new { message = "Geçersiz vardiya ID'si." });
                    }
                    tracking.ShiftID = trackingDto.ShiftID.Value;
                }

                // MovementType kontrolü
                if (!string.IsNullOrEmpty(trackingDto.MovementType))
                {
                    if (trackingDto.MovementType != "Entry" && trackingDto.MovementType != "Exit")
                    {
                        return BadRequest(new { message = "Hareket tipi 'Entry' veya 'Exit' olmalıdır." });
                    }
                    tracking.MovementType = trackingDto.MovementType;
                }

                if (trackingDto.TrackingDateTime.HasValue)
                    tracking.TrackingDateTime = trackingDto.TrackingDateTime.Value;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Takip kaydı başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Takip kaydı güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/Tracking/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTracking(int id)
        {
            try
            {
                var tracking = await _context.Trackings.FindAsync(id);
                if (tracking == null)
                {
                    return NotFound(new { message = "Takip kaydı bulunamadı." });
                }

                _context.Trackings.Remove(tracking);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Takip kaydı başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Takip kaydı silinirken bir hata oluştu.", error = ex.Message });
            }
        }
    }

    // DTOs
    public class TrackingCreateDto
    {
        [Required]
        public int ServiceVehicleID { get; set; }

        [Required]
        public int ShiftID { get; set; }

        [Required]
        public DateTimeOffset TrackingDateTime { get; set; }

        [Required]
        [StringLength(10)]
        public string MovementType { get; set; } = string.Empty; // Entry, Exit
    }

    public class TrackingUpdateDto
    {
        public int? ServiceVehicleID { get; set; }
        public int? ShiftID { get; set; }
        public DateTimeOffset? TrackingDateTime { get; set; }
        
        [StringLength(10)]
        public string? MovementType { get; set; } // Entry, Exit
    }
}