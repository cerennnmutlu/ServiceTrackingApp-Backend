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
    public class VehicleDriverAssignmentController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public VehicleDriverAssignmentController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/VehicleDriverAssignment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleDriverAssignment>>> GetVehicleDriverAssignments()
        {
            try
            {
                var assignments = await _context.VehicleDriverAssignments
                    .Include(vda => vda.ServiceVehicle)
                    .Include(vda => vda.Driver)
                    .OrderByDescending(vda => vda.StartDate)
                    .ToListAsync();
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-şoför atamaları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleDriverAssignment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleDriverAssignment>> GetVehicleDriverAssignment(int id)
        {
            try
            {
                var assignment = await _context.VehicleDriverAssignments
                    .Include(vda => vda.ServiceVehicle)
                    .Include(vda => vda.Driver)
                    .FirstOrDefaultAsync(vda => vda.AssignmentID == id);

                if (assignment == null)
                {
                    return NotFound(new { message = "Araç-şoför ataması bulunamadı." });
                }

                return Ok(assignment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-şoför ataması getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleDriverAssignment/vehicle/5
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<ActionResult<IEnumerable<VehicleDriverAssignment>>> GetAssignmentsByVehicle(int vehicleId)
        {
            try
            {
                var assignments = await _context.VehicleDriverAssignments
                    .Include(vda => vda.ServiceVehicle)
                    .Include(vda => vda.Driver)
                    .Where(vda => vda.ServiceVehicleID == vehicleId)
                    .OrderByDescending(vda => vda.StartDate)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç atamaları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleDriverAssignment/driver/5
        [HttpGet("driver/{driverId}")]
        public async Task<ActionResult<IEnumerable<VehicleDriverAssignment>>> GetAssignmentsByDriver(int driverId)
        {
            try
            {
                var assignments = await _context.VehicleDriverAssignments
                    .Include(vda => vda.ServiceVehicle)
                    .Include(vda => vda.Driver)
                    .Where(vda => vda.DriverID == driverId)
                    .OrderByDescending(vda => vda.StartDate)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şoför atamaları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleDriverAssignment/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<VehicleDriverAssignment>>> GetActiveAssignments()
        {
            try
            {
                var assignments = await _context.VehicleDriverAssignments
                    .Include(vda => vda.ServiceVehicle)
                    .Include(vda => vda.Driver)
                    .Where(vda => vda.EndDate == null || vda.EndDate > DateTime.Now)
                    .OrderByDescending(vda => vda.StartDate)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Aktif atamalar getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/VehicleDriverAssignment
        [HttpPost]
        public async Task<ActionResult<VehicleDriverAssignment>> CreateVehicleDriverAssignment(VehicleDriverAssignmentCreateDto assignmentDto)
        {
            try
            {
                // ServiceVehicle kontrolü
                var vehicle = await _context.ServiceVehicles.FindAsync(assignmentDto.ServiceVehicleID);
                if (vehicle == null)
                {
                    return BadRequest(new { message = "Geçersiz servis aracı ID'si." });
                }

                // Driver kontrolü
                var driver = await _context.Drivers.FindAsync(assignmentDto.DriverID);
                if (driver == null)
                {
                    return BadRequest(new { message = "Geçersiz şoför ID'si." });
                }

                // Aynı araç için aktif atama var mı kontrol et
                var existingActiveAssignment = await _context.VehicleDriverAssignments
                    .FirstOrDefaultAsync(vda => vda.ServiceVehicleID == assignmentDto.ServiceVehicleID && 
                                              (vda.EndDate == null || vda.EndDate > DateTime.Now));
                
                if (existingActiveAssignment != null)
                {
                    return BadRequest(new { message = "Bu araç için zaten aktif bir şoför ataması bulunmaktadır." });
                }

                // Aynı şoför için aktif atama var mı kontrol et
                var existingDriverAssignment = await _context.VehicleDriverAssignments
                    .FirstOrDefaultAsync(vda => vda.DriverID == assignmentDto.DriverID && 
                                              (vda.EndDate == null || vda.EndDate > DateTime.Now));
                
                if (existingDriverAssignment != null)
                {
                    return BadRequest(new { message = "Bu şoför için zaten aktif bir araç ataması bulunmaktadır." });
                }

                var assignment = new VehicleDriverAssignment
                {
                    ServiceVehicleID = assignmentDto.ServiceVehicleID,
                    DriverID = assignmentDto.DriverID,
                    StartDate = assignmentDto.StartDate,
                    EndDate = assignmentDto.EndDate,
                    CreatedAt = DateTime.UtcNow
                };

                _context.VehicleDriverAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // Oluşturulan atamayı ilişkili verilerle birlikte getir
                var createdAssignment = await _context.VehicleDriverAssignments
                    .Include(vda => vda.ServiceVehicle)
                    .Include(vda => vda.Driver)
                    .FirstOrDefaultAsync(vda => vda.AssignmentID == assignment.AssignmentID);

                return CreatedAtAction(nameof(GetVehicleDriverAssignment), new { id = assignment.AssignmentID }, createdAssignment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-şoför ataması oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/VehicleDriverAssignment/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicleDriverAssignment(int id, VehicleDriverAssignmentUpdateDto assignmentDto)
        {
            try
            {
                var assignment = await _context.VehicleDriverAssignments.FindAsync(id);
                if (assignment == null)
                {
                    return NotFound(new { message = "Araç-şoför ataması bulunamadı." });
                }

                // ServiceVehicle kontrolü
                if (assignmentDto.ServiceVehicleID.HasValue)
                {
                    var vehicle = await _context.ServiceVehicles.FindAsync(assignmentDto.ServiceVehicleID.Value);
                    if (vehicle == null)
                    {
                        return BadRequest(new { message = "Geçersiz servis aracı ID'si." });
                    }
                    assignment.ServiceVehicleID = assignmentDto.ServiceVehicleID.Value;
                }

                // Driver kontrolü
                if (assignmentDto.DriverID.HasValue)
                {
                    var driver = await _context.Drivers.FindAsync(assignmentDto.DriverID.Value);
                    if (driver == null)
                    {
                        return BadRequest(new { message = "Geçersiz şoför ID'si." });
                    }
                    assignment.DriverID = assignmentDto.DriverID.Value;
                }

                if (assignmentDto.StartDate.HasValue)
                    assignment.StartDate = assignmentDto.StartDate.Value;

                if (assignmentDto.EndDate.HasValue)
                    assignment.EndDate = assignmentDto.EndDate.Value;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Araç-şoför ataması başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-şoför ataması güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/VehicleDriverAssignment/5/end
        [HttpPut("{id}/end")]
        public async Task<IActionResult> EndAssignment(int id)
        {
            try
            {
                var assignment = await _context.VehicleDriverAssignments.FindAsync(id);
                if (assignment == null)
                {
                    return NotFound(new { message = "Araç-şoför ataması bulunamadı." });
                }

                if (assignment.EndDate != null && assignment.EndDate <= DateTime.Now)
                {
                    return BadRequest(new { message = "Bu atama zaten sonlandırılmış." });
                }

                assignment.EndDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Araç-şoför ataması başarıyla sonlandırıldı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Atama sonlandırılırken bir hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/VehicleDriverAssignment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicleDriverAssignment(int id)
        {
            try
            {
                var assignment = await _context.VehicleDriverAssignments.FindAsync(id);
                if (assignment == null)
                {
                    return NotFound(new { message = "Araç-şoför ataması bulunamadı." });
                }

                _context.VehicleDriverAssignments.Remove(assignment);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Araç-şoför ataması başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-şoför ataması silinirken bir hata oluştu.", error = ex.Message });
            }
        }
    }

    // DTOs
    public class VehicleDriverAssignmentCreateDto
    {
        [Required]
        public int ServiceVehicleID { get; set; }

        [Required]
        public int DriverID { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }

    public class VehicleDriverAssignmentUpdateDto
    {
        public int? ServiceVehicleID { get; set; }
        public int? DriverID { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}