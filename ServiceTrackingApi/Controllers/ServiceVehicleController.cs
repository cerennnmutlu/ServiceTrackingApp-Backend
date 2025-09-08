using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Models;
using ServiceTrackingApi.Data;
using Microsoft.AspNetCore.Authorization;

namespace ServiceTrackingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceVehicleController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public ServiceVehicleController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/ServiceVehicle
        [HttpGet]
        [Authorize(Roles = "Admin,ShiftManager,SecurityGuard")]
        public async Task<ActionResult<IEnumerable<ServiceVehicle>>> GetServiceVehicles()
        {
            try
            {
                var vehicles = await _context.ServiceVehicles
                    .Include(v => v.Route)
                    .ToListAsync();
                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Servis araçları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/ServiceVehicle/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ShiftManager,SecurityGuard")]
        public async Task<ActionResult<ServiceVehicle>> GetServiceVehicle(int id)
        {
            try
            {
                var vehicle = await _context.ServiceVehicles
                    .Include(v => v.Route)
                    .FirstOrDefaultAsync(v => v.ServiceVehicleID == id);

                if (vehicle == null)
                {
                    return NotFound(new { message = "Servis aracı bulunamadı." });
                }

                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Servis aracı getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/ServiceVehicle
        [HttpPost]
        [Authorize(Roles = "Admin,ShiftManager")]
        public async Task<ActionResult<ServiceVehicle>> CreateServiceVehicle(ServiceVehicleCreateDto vehicleDto)
        {
            try
            {
                // Plaka numarası kontrolü
                var existingVehicle = await _context.ServiceVehicles
                    .FirstOrDefaultAsync(v => v.PlateNumber == vehicleDto.PlateNumber);
                
                if (existingVehicle != null)
                {
                    return BadRequest(new { message = "Bu plaka numarası zaten kayıtlı." });
                }

                // Route kontrolü
                var route = await _context.Routes.FindAsync(vehicleDto.RouteID);
                if (route == null)
                {
                    return BadRequest(new { message = "Geçersiz rota ID'si." });
                }

                var vehicle = new ServiceVehicle
                {
                    PlateNumber = vehicleDto.PlateNumber,
                    Brand = vehicleDto.Brand,
                    Model = vehicleDto.Model,
                    Capacity = vehicleDto.Capacity,
                    Status = vehicleDto.Status ?? "Active",
                    RouteID = vehicleDto.RouteID,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ServiceVehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                // Oluşturulan aracı route bilgisiyle birlikte getir
                var createdVehicle = await _context.ServiceVehicles
                    .Include(v => v.Route)
                    .FirstOrDefaultAsync(v => v.ServiceVehicleID == vehicle.ServiceVehicleID);

                return CreatedAtAction(nameof(GetServiceVehicle), new { id = vehicle.ServiceVehicleID }, createdVehicle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Servis aracı oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/ServiceVehicle/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ShiftManager")]
        public async Task<IActionResult> UpdateServiceVehicle(int id, ServiceVehicleUpdateDto vehicleDto)
        {
            try
            {
                var vehicle = await _context.ServiceVehicles.FindAsync(id);
                if (vehicle == null)
                {
                    return NotFound(new { message = "Servis aracı bulunamadı." });
                }

                // Plaka numarası kontrolü (kendisi hariç)
                if (!string.IsNullOrEmpty(vehicleDto.PlateNumber) && vehicleDto.PlateNumber != vehicle.PlateNumber)
                {
                    var existingVehicle = await _context.ServiceVehicles
                        .FirstOrDefaultAsync(v => v.PlateNumber == vehicleDto.PlateNumber && v.ServiceVehicleID != id);
                    
                    if (existingVehicle != null)
                    {
                        return BadRequest(new { message = "Bu plaka numarası zaten kayıtlı." });
                    }
                }

                // Route kontrolü
                if (vehicleDto.RouteID.HasValue)
                {
                    var route = await _context.Routes.FindAsync(vehicleDto.RouteID.Value);
                    if (route == null)
                    {
                        return BadRequest(new { message = "Geçersiz rota ID'si." });
                    }
                    vehicle.RouteID = vehicleDto.RouteID.Value;
                }

                // Güncelleme
                if (!string.IsNullOrEmpty(vehicleDto.PlateNumber))
                    vehicle.PlateNumber = vehicleDto.PlateNumber;
                
                if (vehicleDto.Brand != null)
                    vehicle.Brand = vehicleDto.Brand;
                
                if (vehicleDto.Model != null)
                    vehicle.Model = vehicleDto.Model;
                
                if (vehicleDto.Capacity.HasValue)
                    vehicle.Capacity = vehicleDto.Capacity.Value;
                
                if (!string.IsNullOrEmpty(vehicleDto.Status))
                    vehicle.Status = vehicleDto.Status;

                vehicle.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Servis aracı başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Servis aracı güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/ServiceVehicle/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteServiceVehicle(int id)
        {
            try
            {
                var vehicle = await _context.ServiceVehicles.FindAsync(id);
                if (vehicle == null)
                {
                    return NotFound(new { message = "Servis aracı bulunamadı." });
                }

                // İlişkili kayıtları kontrol et
                var hasAssignments = await _context.VehicleDriverAssignments
                    .AnyAsync(vda => vda.ServiceVehicleID == id);
                
                var hasShiftAssignments = await _context.VehicleShiftAssignments
                    .AnyAsync(vsa => vsa.ServiceVehicleID == id);
                
                var hasTrackings = await _context.Trackings
                    .AnyAsync(t => t.ServiceVehicleID == id);

                if (hasAssignments || hasShiftAssignments || hasTrackings)
                {
                    return BadRequest(new { message = "Bu servis aracı başka kayıtlarda kullanıldığı için silinemez. Önce durumunu 'Inactive' olarak değiştirin." });
                }

                _context.ServiceVehicles.Remove(vehicle);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Servis aracı başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Servis aracı silinirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/ServiceVehicle/by-route/{routeId}
        [HttpGet("by-route/{routeId}")]
        [Authorize(Roles = "Admin,ShiftManager,SecurityGuard")]
        public async Task<ActionResult<IEnumerable<ServiceVehicle>>> GetServiceVehiclesByRoute(int routeId)
        {
            try
            {
                var vehicles = await _context.ServiceVehicles
                    .Include(v => v.Route)
                    .Where(v => v.RouteID == routeId)
                    .ToListAsync();

                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Rota araçları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/ServiceVehicle/active
        [HttpGet("active")]
        [Authorize(Roles = "Admin,ShiftManager,SecurityGuard")]
        public async Task<ActionResult<IEnumerable<ServiceVehicle>>> GetActiveServiceVehicles()
        {
            try
            {
                var vehicles = await _context.ServiceVehicles
                    .Include(v => v.Route)
                    .Where(v => v.Status == "Active")
                    .ToListAsync();

                return Ok(vehicles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Aktif servis araçları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }
    }

    // DTO Classes
    public class ServiceVehicleCreateDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int Capacity { get; set; }
        public string? Status { get; set; }
        public int RouteID { get; set; }
    }

    public class ServiceVehicleUpdateDto
    {
        public string? PlateNumber { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int? Capacity { get; set; }
        public string? Status { get; set; }
        public int? RouteID { get; set; }
    }
}