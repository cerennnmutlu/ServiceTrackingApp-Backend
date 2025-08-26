using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Data;
using ServiceTrackingApi.Models;
using RouteModel = ServiceTrackingApi.Models.Route;

namespace ServiceTrackingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public RouteController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/Route
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteModel>>> GetRoutes()
        {
            return await _context.Routes
                .Include(r => r.ServiceVehicles)
                .ToListAsync();
        }

        // GET: api/Route/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RouteModel>> GetRoute(int id)
        {
            var route = await _context.Routes
                .Include(r => r.ServiceVehicles)
                .FirstOrDefaultAsync(r => r.RouteID == id);

            if (route == null)
            {
                return NotFound();
            }

            return route;
        }

        // PUT: api/Route/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRoute(int id, RouteModel route)
        {
            if (id != route.RouteID)
            {
                return BadRequest();
            }

            // Validate distance and duration
            if (route.Distance.HasValue && route.Distance < 0)
            {
                return BadRequest("Distance cannot be negative");
            }

            if (route.EstimatedDuration.HasValue && route.EstimatedDuration < 0)
            {
                return BadRequest("Estimated duration cannot be negative");
            }

            route.UpdatedAt = DateTime.UtcNow;
            _context.Entry(route).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RouteExists(id))
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

        // POST: api/Route
        [HttpPost]
        public async Task<ActionResult<RouteModel>> PostRoute(RouteModel route)
        {
            // Check if route name already exists
            var nameExists = await _context.Routes
                .AnyAsync(r => r.RouteName == route.RouteName);
            if (nameExists)
            {
                return BadRequest("Route name already exists");
            }

            // Validate distance and duration
            if (route.Distance.HasValue && route.Distance < 0)
            {
                return BadRequest("Distance cannot be negative");
            }

            if (route.EstimatedDuration.HasValue && route.EstimatedDuration < 0)
            {
                return BadRequest("Estimated duration cannot be negative");
            }

            route.CreatedAt = DateTime.UtcNow;
            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRoute", new { id = route.RouteID }, route);
        }

        // DELETE: api/Route/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                return NotFound();
            }

            // Check if route has assigned vehicles
            var hasAssignedVehicles = await _context.ServiceVehicles
                .AnyAsync(sv => sv.RouteID == id);

            if (hasAssignedVehicles)
            {
                return BadRequest("Cannot delete route with assigned vehicles");
            }

            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Route/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<RouteModel>>> GetActiveRoutes()
        {
            return await _context.Routes
                .Where(r => r.Status == "Active")
                .Include(r => r.ServiceVehicles)
                .ToListAsync();
        }

        // GET: api/Route/vehicles/{id}
        [HttpGet("vehicles/{id}")]
        public async Task<ActionResult<IEnumerable<ServiceVehicle>>> GetRouteVehicles(int id)
        {
            var vehicles = await _context.ServiceVehicles
                .Where(sv => sv.RouteID == id)
                .ToListAsync();

            return vehicles;
        }

        // GET: api/Route/statistics/{id}
        [HttpGet("statistics/{id}")]
        public async Task<ActionResult<object>> GetRouteStatistics(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                return NotFound();
            }

            var vehicleCount = await _context.ServiceVehicles
                .CountAsync(sv => sv.RouteID == id);

            var activeVehicleCount = await _context.ServiceVehicles
                .CountAsync(sv => sv.RouteID == id && sv.Status == "Active");

            var totalCapacity = await _context.ServiceVehicles
                .Where(sv => sv.RouteID == id && sv.Status == "Active")
                .SumAsync(sv => sv.Capacity);

            return new
            {
                RouteID = id,
                RouteName = route.RouteName,
                TotalVehicles = vehicleCount,
                ActiveVehicles = activeVehicleCount,
                TotalCapacity = totalCapacity,
                Distance = route.Distance,
                EstimatedDuration = route.EstimatedDuration
            };
        }

        // PUT: api/Route/status/{id}
        [HttpPut("status/{id}")]
        public async Task<IActionResult> UpdateRouteStatus(int id, [FromBody] string status)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                return NotFound();
            }

            if (status != "Active" && status != "Inactive")
            {
                return BadRequest("Status must be 'Active' or 'Inactive'");
            }

            route.Status = status;
            route.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RouteExists(int id)
        {
            return _context.Routes.Any(e => e.RouteID == id);
        }
    }
}