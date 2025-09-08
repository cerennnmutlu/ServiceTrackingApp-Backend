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
    public class DriverController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public DriverController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/Driver
        [HttpGet]
        [Authorize(Roles = "Admin,ShiftManager")]
        public async Task<ActionResult<IEnumerable<Driver>>> GetDrivers()
        {
            try
            {
                var drivers = await _context.Drivers
                    .Include(d => d.VehicleDriverAssignments)
                        .ThenInclude(vda => vda.ServiceVehicle)
                    .ToListAsync();
                return Ok(drivers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şoförler getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Driver/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ShiftManager")]
        public async Task<ActionResult<Driver>> GetDriver(int id)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.VehicleDriverAssignments)
                        .ThenInclude(vda => vda.ServiceVehicle)
                    .FirstOrDefaultAsync(d => d.DriverID == id);

                if (driver == null)
                {
                    return NotFound(new { message = "Şoför bulunamadı." });
                }

                return Ok(driver);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şoför getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/Driver
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Driver>> CreateDriver(DriverCreateDto driverDto)
        {
            try
            {
                // Check if driver with same name already exists
                var existingDriver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.FullName == driverDto.FullName);
                
                if (existingDriver != null)
                {
                    return BadRequest(new { message = "Bu şoför adı zaten kullanılıyor." });
                }

                var driver = new Driver
                {
                    FullName = driverDto.FullName,
                    Phone = driverDto.Phone,
                    Status = driverDto.Status ?? "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDriver), new { id = driver.DriverID }, driver);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şoför oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/Driver/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDriver(int id, DriverUpdateDto driverDto)
        {
            try
            {
                var driver = await _context.Drivers.FindAsync(id);
                if (driver == null)
                {
                    return NotFound(new { message = "Şoför bulunamadı." });
                }

                // Check if driver name already exists (excluding current driver)
                var existingDriver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.FullName == driverDto.FullName && d.DriverID != id);
                
                if (existingDriver != null)
                {
                    return BadRequest(new { message = "Bu şoför adı zaten kullanılıyor." });
                }

                driver.FullName = driverDto.FullName;
                driver.Phone = driverDto.Phone;
                driver.Status = driverDto.Status ?? driver.Status;
                driver.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Şoför başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şoför güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/Driver/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            try
            {
                var driver = await _context.Drivers
                    .Include(d => d.VehicleDriverAssignments)
                    .FirstOrDefaultAsync(d => d.DriverID == id);
                
                if (driver == null)
                {
                    return NotFound(new { message = "Şoför bulunamadı." });
                }

                // Check if driver has associated vehicle assignments
                if (driver.VehicleDriverAssignments.Any())
                {
                    return BadRequest(new { message = "Bu şoföre bağlı araç atamaları bulunduğu için silinemez." });
                }

                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Şoför başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şoför silinirken bir hata oluştu.", error = ex.Message });
            }
        }
    }

    // DTOs
    public class DriverCreateDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }
    }

    public class DriverUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }
    }
}