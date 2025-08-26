using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Data;
using ServiceTrackingApi.Models;

namespace ServiceTrackingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceVehicleController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public ServiceVehicleController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/ServiceVehicle
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ServiceVehicle>>> GetServiceVehicles()
        {
            return await _context.ServiceVehicles
                .Include(sv => sv.Route)
                .ToListAsync();
        }

        // GET: api/ServiceVehicle/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceVehicle>> GetServiceVehicle(int id)
        {
            var serviceVehicle = await _context.ServiceVehicles
                .Include(sv => sv.Route)
                .Include(sv => sv.VehicleDriverAssignments)
                .Include(sv => sv.VehicleShiftAssignments)
                .FirstOrDefaultAsync(sv => sv.ServiceVehicleID == id);

            if (serviceVehicle == null)
            {
                return NotFound();
            }

            return serviceVehicle;
        }

        // PUT: api/ServiceVehicle/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutServiceVehicle(int id, ServiceVehicle serviceVehicle)
        {
            if (id != serviceVehicle.ServiceVehicleID)
            {
                return BadRequest();
            }

            // Check if route exists
            var routeExists = await _context.Routes.AnyAsync(r => r.RouteID == serviceVehicle.RouteID);
            if (!routeExists)
            {
                return BadRequest("Invalid RouteID");
            }

            serviceVehicle.UpdatedAt = DateTime.UtcNow;
            _context.Entry(serviceVehicle).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceVehicleExists(id))
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

        // POST: api/ServiceVehicle
        [HttpPost]
        public async Task<ActionResult<ServiceVehicle>> PostServiceVehicle(ServiceVehicle serviceVehicle)
        {
            // Check if route exists
            var routeExists = await _context.Routes.AnyAsync(r => r.RouteID == serviceVehicle.RouteID);
            if (!routeExists)
            {
                return BadRequest("Invalid RouteID");
            }

            // Check if plate number already exists
            var plateExists = await _context.ServiceVehicles
                .AnyAsync(sv => sv.PlateNumber == serviceVehicle.PlateNumber);
            if (plateExists)
            {
                return BadRequest("Plate number already exists");
            }

            serviceVehicle.CreatedAt = DateTime.UtcNow;
            _context.ServiceVehicles.Add(serviceVehicle);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetServiceVehicle", new { id = serviceVehicle.ServiceVehicleID }, serviceVehicle);
        }

        // DELETE: api/ServiceVehicle/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteServiceVehicle(int id)
        {
            var serviceVehicle = await _context.ServiceVehicles.FindAsync(id);
            if (serviceVehicle == null)
            {
                return NotFound();
            }

            // Check if vehicle has active assignments
            var hasActiveDriverAssignments = await _context.VehicleDriverAssignments
                .AnyAsync(vda => vda.ServiceVehicleID == id && vda.EndDate == null);
            var hasActiveShiftAssignments = await _context.VehicleShiftAssignments
                .AnyAsync(vsa => vsa.ServiceVehicleID == id && vsa.AssignmentDate >= DateTime.Today);

            if (hasActiveDriverAssignments || hasActiveShiftAssignments)
            {
                return BadRequest("Cannot delete vehicle with active assignments");
            }

            _context.ServiceVehicles.Remove(serviceVehicle);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/ServiceVehicle/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<ServiceVehicle>>> GetActiveServiceVehicles()
        {
            return await _context.ServiceVehicles
                .Include(sv => sv.Route)
                .Where(sv => sv.Status == "Active")
                .ToListAsync();
        }

        // GET: api/ServiceVehicle/by-route/{routeId}
        [HttpGet("by-route/{routeId}")]
        public async Task<ActionResult<IEnumerable<ServiceVehicle>>> GetServiceVehiclesByRoute(int routeId)
        {
            return await _context.ServiceVehicles
                .Include(sv => sv.Route)
                .Where(sv => sv.RouteID == routeId)
                .ToListAsync();
        }

        private bool ServiceVehicleExists(int id)
        {
            return _context.ServiceVehicles.Any(e => e.ServiceVehicleID == id);
        }
    }
}