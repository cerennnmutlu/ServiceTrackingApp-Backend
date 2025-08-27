using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Models;
using ServiceTrackingApi.Data;
using Microsoft.AspNetCore.Authorization;
using RouteModel = ServiceTrackingApi.Models.Route;
using System.ComponentModel.DataAnnotations;

namespace ServiceTrackingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
            try
            {
                var routes = await _context.Routes
                    .Include(r => r.ServiceVehicles)
                    .ToListAsync();
                return Ok(routes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Güzergahlar getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Route/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RouteModel>> GetRoute(int id)
        {
            try
            {
                var route = await _context.Routes
                    .Include(r => r.ServiceVehicles)
                    .FirstOrDefaultAsync(r => r.RouteID == id);

                if (route == null)
                {
                    return NotFound(new { message = "Güzergah bulunamadı." });
                }

                return Ok(route);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Güzergah getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/Route
        [HttpPost]
        public async Task<ActionResult<RouteModel>> CreateRoute(RouteCreateDto routeDto)
        {
            try
            {
                // Check if route name already exists
                var existingRoute = await _context.Routes
                    .FirstOrDefaultAsync(r => r.RouteName == routeDto.RouteName);
                
                if (existingRoute != null)
                {
                    return BadRequest(new { message = "Bu güzergah adı zaten kullanılıyor." });
                }

                var route = new RouteModel
                {
                    RouteName = routeDto.RouteName,
                    Description = routeDto.Description,
                    Distance = routeDto.Distance,
                    EstimatedDuration = routeDto.EstimatedDuration,
                    Status = routeDto.Status ?? "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Routes.Add(route);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRoute), new { id = route.RouteID }, route);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Güzergah oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/Route/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoute(int id, RouteUpdateDto routeDto)
        {
            try
            {
                var route = await _context.Routes.FindAsync(id);
                if (route == null)
                {
                    return NotFound(new { message = "Güzergah bulunamadı." });
                }

                // Check if route name already exists (excluding current route)
                var existingRoute = await _context.Routes
                    .FirstOrDefaultAsync(r => r.RouteName == routeDto.RouteName && r.RouteID != id);
                
                if (existingRoute != null)
                {
                    return BadRequest(new { message = "Bu güzergah adı zaten kullanılıyor." });
                }

                route.RouteName = routeDto.RouteName;
                route.Description = routeDto.Description;
                route.Distance = routeDto.Distance;
                route.EstimatedDuration = routeDto.EstimatedDuration;
                route.Status = routeDto.Status ?? route.Status;
                route.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Güzergah başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Güzergah güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/Route/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            try
            {
                var route = await _context.Routes
                    .Include(r => r.ServiceVehicles)
                    .FirstOrDefaultAsync(r => r.RouteID == id);
                
                if (route == null)
                {
                    return NotFound(new { message = "Güzergah bulunamadı." });
                }

                // Check if route has associated service vehicles
                if (route.ServiceVehicles.Any())
                {
                    return BadRequest(new { message = "Bu güzergaha bağlı servis araçları bulunduğu için silinemez." });
                }

                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Güzergah başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Güzergah silinirken bir hata oluştu.", error = ex.Message });
            }
        }
    }

    // DTOs
    public class RouteCreateDto
    {
        [Required]
        [StringLength(100)]
        public string RouteName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public decimal? Distance { get; set; }

        public int? EstimatedDuration { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }
    }

    public class RouteUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string RouteName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public decimal? Distance { get; set; }

        public int? EstimatedDuration { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }
    }
}