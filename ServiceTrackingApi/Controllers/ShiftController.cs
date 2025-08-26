using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Data;
using ServiceTrackingApi.Models;

namespace ServiceTrackingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public ShiftController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/Shift
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shift>>> GetShifts()
        {
            return await _context.Shifts.ToListAsync();
        }

        // GET: api/Shift/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Shift>> GetShift(int id)
        {
            var shift = await _context.Shifts
                .Include(s => s.VehicleShiftAssignments)
                .ThenInclude(vsa => vsa.ServiceVehicle)
                .FirstOrDefaultAsync(s => s.ShiftID == id);

            if (shift == null)
            {
                return NotFound();
            }

            return shift;
        }

        // PUT: api/Shift/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutShift(int id, Shift shift)
        {
            if (id != shift.ShiftID)
            {
                return BadRequest();
            }

            // Validate time range
            if (shift.StartTime >= shift.EndTime)
            {
                return BadRequest("Start time must be before end time");
            }

            shift.UpdatedAt = DateTime.UtcNow;
            _context.Entry(shift).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShiftExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Shift
        [HttpPost]
        public async Task<ActionResult<Shift>> PostShift(Shift shift)
        {
            // Validate time range
            if (shift.StartTime >= shift.EndTime)
            {
                return BadRequest("Start time must be before end time");
            }

            // Check if shift name already exists
            var nameExists = await _context.Shifts
                .AnyAsync(s => s.ShiftName == shift.ShiftName);
            if (nameExists)
            {
                return BadRequest("Shift name already exists");
            }

            // Check for overlapping shifts
            var overlappingShift = await _context.Shifts
                .AnyAsync(s => s.Status == "Active" && 
                    ((shift.StartTime >= s.StartTime && shift.StartTime < s.EndTime) ||
                     (shift.EndTime > s.StartTime && shift.EndTime <= s.EndTime) ||
                     (shift.StartTime <= s.StartTime && shift.EndTime >= s.EndTime)));

            if (overlappingShift)
            {
                return BadRequest("Shift times overlap with existing active shift");
            }

            shift.CreatedAt = DateTime.UtcNow;
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetShift", new { id = shift.ShiftID }, shift);
        }

        // DELETE: api/Shift/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
            {
                return NotFound();
            }

            // Check if shift has active assignments
            var hasActiveAssignments = await _context.VehicleShiftAssignments
                .AnyAsync(vsa => vsa.ShiftID == id && vsa.AssignmentDate >= DateTime.Today);

            if (hasActiveAssignments)
            {
                return BadRequest("Cannot delete shift with active assignments");
            }

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Shift/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Shift>>> GetActiveShifts()
        {
            return await _context.Shifts
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        // GET: api/Shift/current
        [HttpGet("current")]
        public async Task<ActionResult<Shift>> GetCurrentShift()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            
            var currentShift = await _context.Shifts
                .Where(s => s.Status == "Active" && 
                           s.StartTime <= currentTime && 
                           s.EndTime > currentTime)
                .FirstOrDefaultAsync();

            if (currentShift == null)
            {
                return NotFound("No active shift found for current time");
            }

            return currentShift;
        }

        // GET: api/Shift/assignments/{id}
        [HttpGet("assignments/{id}")]
        public async Task<ActionResult<IEnumerable<VehicleShiftAssignment>>> GetShiftAssignments(int id)
        {
            var assignments = await _context.VehicleShiftAssignments
                .Include(vsa => vsa.ServiceVehicle)
                .Where(vsa => vsa.ShiftID == id)
                .ToListAsync();

            return assignments;
        }

        private bool ShiftExists(int id)
        {
            return _context.Shifts.Any(e => e.ShiftID == id);
        }
    }
}