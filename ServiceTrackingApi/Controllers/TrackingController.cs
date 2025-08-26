using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Data;
using ServiceTrackingApi.Models;

namespace ServiceTrackingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            return await _context.Trackings
                .Include(t => t.ServiceVehicle)
                .Include(t => t.Shift)
                .OrderByDescending(t => t.TrackingDateTime)
                .ToListAsync();
        }

        // GET: api/Tracking/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Tracking>> GetTracking(int id)
        {
            var tracking = await _context.Trackings
                .Include(t => t.ServiceVehicle)
                .Include(t => t.Shift)
                .FirstOrDefaultAsync(t => t.TrackingID == id);

            if (tracking == null)
            {
                return NotFound();
            }

            return tracking;
        }

        // POST: api/Tracking/entry
        [HttpPost("entry")]
        public async Task<ActionResult<Tracking>> RecordEntry([FromBody] TrackingRequest request)
        {
            // Validate vehicle exists and is active
            var vehicle = await _context.ServiceVehicles
                .FirstOrDefaultAsync(sv => sv.ServiceVehicleID == request.ServiceVehicleID && sv.Status == "Active");
            if (vehicle == null)
            {
                return BadRequest("Vehicle not found or inactive");
            }

            // Validate shift exists and is active
            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.ShiftID == request.ShiftID && s.Status == "Active");
            if (shift == null)
            {
                return BadRequest("Shift not found or inactive");
            }

            // Check if vehicle is already entered (no exit record after last entry)
            var lastTracking = await _context.Trackings
                .Where(t => t.ServiceVehicleID == request.ServiceVehicleID)
                .OrderByDescending(t => t.TrackingDateTime)
                .FirstOrDefaultAsync();

            if (lastTracking != null && lastTracking.MovementType == "Entry")
            {
                return BadRequest("Vehicle is already entered. Please record exit first.");
            }

            var tracking = new Tracking
            {
                ServiceVehicleID = request.ServiceVehicleID,
                ShiftID = request.ShiftID,
                TrackingDateTime = DateTimeOffset.Now,
                MovementType = "Entry",
                CreatedAt = DateTime.UtcNow
            };

            _context.Trackings.Add(tracking);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTracking", new { id = tracking.TrackingID }, tracking);
        }

        // POST: api/Tracking/exit
        [HttpPost("exit")]
        public async Task<ActionResult<Tracking>> RecordExit([FromBody] TrackingRequest request)
        {
            // Validate vehicle exists
            var vehicle = await _context.ServiceVehicles
                .FirstOrDefaultAsync(sv => sv.ServiceVehicleID == request.ServiceVehicleID);
            if (vehicle == null)
            {
                return BadRequest("Vehicle not found");
            }

            // Validate shift exists
            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.ShiftID == request.ShiftID);
            if (shift == null)
            {
                return BadRequest("Shift not found");
            }

            // Check if vehicle has an entry record without exit
            var lastTracking = await _context.Trackings
                .Where(t => t.ServiceVehicleID == request.ServiceVehicleID)
                .OrderByDescending(t => t.TrackingDateTime)
                .FirstOrDefaultAsync();

            if (lastTracking == null || lastTracking.MovementType == "Exit")
            {
                return BadRequest("Vehicle is not entered. Please record entry first.");
            }

            var tracking = new Tracking
            {
                ServiceVehicleID = request.ServiceVehicleID,
                ShiftID = request.ShiftID,
                TrackingDateTime = DateTimeOffset.Now,
                MovementType = "Exit",
                CreatedAt = DateTime.UtcNow
            };

            _context.Trackings.Add(tracking);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTracking", new { id = tracking.TrackingID }, tracking);
        }

        // GET: api/Tracking/vehicle/{vehicleId}
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<ActionResult<IEnumerable<Tracking>>> GetVehicleTrackings(int vehicleId)
        {
            return await _context.Trackings
                .Include(t => t.ServiceVehicle)
                .Include(t => t.Shift)
                .Where(t => t.ServiceVehicleID == vehicleId)
                .OrderByDescending(t => t.TrackingDateTime)
                .ToListAsync();
        }

        // GET: api/Tracking/shift/{shiftId}
        [HttpGet("shift/{shiftId}")]
        public async Task<ActionResult<IEnumerable<Tracking>>> GetShiftTrackings(int shiftId)
        {
            return await _context.Trackings
                .Include(t => t.ServiceVehicle)
                .Include(t => t.Shift)
                .Where(t => t.ShiftID == shiftId)
                .OrderByDescending(t => t.TrackingDateTime)
                .ToListAsync();
        }

        // GET: api/Tracking/active-vehicles
        [HttpGet("active-vehicles")]
        public async Task<ActionResult<IEnumerable<object>>> GetActiveVehicles()
        {
            var activeVehicles = await _context.Trackings
                .Include(t => t.ServiceVehicle)
                .Include(t => t.Shift)
                .GroupBy(t => t.ServiceVehicleID)
                .Select(g => g.OrderByDescending(t => t.TrackingDateTime).First())
                .Where(t => t.MovementType == "Entry")
                .Select(t => new
                {
                    VehicleID = t.ServiceVehicleID,
                    PlateNumber = t.ServiceVehicle.PlateNumber,
                    ShiftName = t.Shift.ShiftName,
                    EntryTime = t.TrackingDateTime,
                    Status = "Active"
                })
                .ToListAsync();

            return Ok(activeVehicles);
        }

        // GET: api/Tracking/daily-report
        [HttpGet("daily-report")]
        public async Task<ActionResult<object>> GetDailyReport([FromQuery] DateTime? date = null)
        {
            var reportDate = date ?? DateTime.Today;
            var startDate = reportDate.Date;
            var endDate = startDate.AddDays(1);

            var trackings = await _context.Trackings
                .Include(t => t.ServiceVehicle)
                .Include(t => t.Shift)
                .Where(t => t.TrackingDateTime >= startDate && t.TrackingDateTime < endDate)
                .OrderBy(t => t.TrackingDateTime)
                .ToListAsync();

            var entries = trackings.Where(t => t.MovementType == "Entry").Count();
            var exits = trackings.Where(t => t.MovementType == "Exit").Count();
            var activeVehicles = entries - exits;

            var vehicleDetails = trackings
                .GroupBy(t => t.ServiceVehicleID)
                .Select(g => new
                {
                    VehicleID = g.Key,
                    PlateNumber = g.First().ServiceVehicle.PlateNumber,
                    Entries = g.Count(t => t.MovementType == "Entry"),
                    Exits = g.Count(t => t.MovementType == "Exit"),
                    LastActivity = g.Max(t => t.TrackingDateTime)
                })
                .ToList();

            return new
            {
                Date = reportDate.ToString("yyyy-MM-dd"),
                TotalEntries = entries,
                TotalExits = exits,
                CurrentlyActive = activeVehicles,
                VehicleDetails = vehicleDetails
            };
        }

        // GET: api/Tracking/vehicle-status/{vehicleId}
        [HttpGet("vehicle-status/{vehicleId}")]
        public async Task<ActionResult<object>> GetVehicleStatus(int vehicleId)
        {
            var lastTracking = await _context.Trackings
                .Include(t => t.ServiceVehicle)
                .Include(t => t.Shift)
                .Where(t => t.ServiceVehicleID == vehicleId)
                .OrderByDescending(t => t.TrackingDateTime)
                .FirstOrDefaultAsync();

            if (lastTracking == null)
            {
                return new
                {
                    VehicleID = vehicleId,
                    Status = "No Activity",
                    LastActivity = (DateTimeOffset?)null
                };
            }

            return new
            {
                VehicleID = vehicleId,
                PlateNumber = lastTracking.ServiceVehicle.PlateNumber,
                Status = lastTracking.MovementType == "Entry" ? "Active" : "Inactive",
                LastActivity = lastTracking.TrackingDateTime,
                ShiftName = lastTracking.Shift.ShiftName
            };
        }

        // DELETE: api/Tracking/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTracking(int id)
        {
            var tracking = await _context.Trackings.FindAsync(id);
            if (tracking == null)
            {
                return NotFound();
            }

            _context.Trackings.Remove(tracking);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class TrackingRequest
    {
        public int ServiceVehicleID { get; set; }
        public int ShiftID { get; set; }
    }
}