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
    public class VehicleShiftAssignmentController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public VehicleShiftAssignmentController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/VehicleShiftAssignment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleShiftAssignment>>> GetVehicleShiftAssignments()
        {
            try
            {
                var assignments = await _context.VehicleShiftAssignments
                    .Include(vsa => vsa.ServiceVehicle)
                    .Include(vsa => vsa.Shift)
                    .OrderByDescending(vsa => vsa.AssignmentDate)
                    .ToListAsync();
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-vardiya atamaları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleShiftAssignment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleShiftAssignment>> GetVehicleShiftAssignment(int id)
        {
            try
            {
                var assignment = await _context.VehicleShiftAssignments
                    .Include(vsa => vsa.ServiceVehicle)
                    .Include(vsa => vsa.Shift)
                    .FirstOrDefaultAsync(vsa => vsa.AssignmentID == id);

                if (assignment == null)
                {
                    return NotFound(new { message = "Araç-vardiya ataması bulunamadı." });
                }

                return Ok(assignment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-vardiya ataması getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleShiftAssignment/vehicle/5
        [HttpGet("vehicle/{vehicleId}")]
        public async Task<ActionResult<IEnumerable<VehicleShiftAssignment>>> GetAssignmentsByVehicle(int vehicleId)
        {
            try
            {
                var assignments = await _context.VehicleShiftAssignments
                    .Include(vsa => vsa.ServiceVehicle)
                    .Include(vsa => vsa.Shift)
                    .Where(vsa => vsa.ServiceVehicleID == vehicleId)
                    .OrderByDescending(vsa => vsa.AssignmentDate)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç atamaları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleShiftAssignment/shift/5
        [HttpGet("shift/{shiftId}")]
        public async Task<ActionResult<IEnumerable<VehicleShiftAssignment>>> GetAssignmentsByShift(int shiftId)
        {
            try
            {
                var assignments = await _context.VehicleShiftAssignments
                    .Include(vsa => vsa.ServiceVehicle)
                    .Include(vsa => vsa.Shift)
                    .Where(vsa => vsa.ShiftID == shiftId)
                    .OrderByDescending(vsa => vsa.AssignmentDate)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Vardiya atamaları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleShiftAssignment/date/2025-01-15
        [HttpGet("date/{date}")]
        public async Task<ActionResult<IEnumerable<VehicleShiftAssignment>>> GetAssignmentsByDate(DateTime date)
        {
            try
            {
                var assignments = await _context.VehicleShiftAssignments
                    .Include(vsa => vsa.ServiceVehicle)
                    .Include(vsa => vsa.Shift)
                    .Where(vsa => vsa.AssignmentDate.Date == date.Date)
                    .OrderBy(vsa => vsa.Shift.StartTime)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Tarih atamaları getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/VehicleShiftAssignment/today
        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<VehicleShiftAssignment>>> GetTodayAssignments()
        {
            try
            {
                var today = DateTime.Today;
                var assignments = await _context.VehicleShiftAssignments
                    .Include(vsa => vsa.ServiceVehicle)
                    .Include(vsa => vsa.Shift)
                    .Where(vsa => vsa.AssignmentDate.Date == today)
                    .OrderBy(vsa => vsa.Shift.StartTime)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bugünkü atamalar getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/VehicleShiftAssignment
        [HttpPost]
        public async Task<ActionResult<VehicleShiftAssignment>> CreateVehicleShiftAssignment(VehicleShiftAssignmentCreateDto assignmentDto)
        {
            try
            {
                // ServiceVehicle kontrolü
                var vehicle = await _context.ServiceVehicles.FindAsync(assignmentDto.ServiceVehicleID);
                if (vehicle == null)
                {
                    return BadRequest(new { message = "Geçersiz servis aracı ID'si." });
                }

                // Shift kontrolü
                var shift = await _context.Shifts.FindAsync(assignmentDto.ShiftID);
                if (shift == null)
                {
                    return BadRequest(new { message = "Geçersiz vardiya ID'si." });
                }

                // Aynı araç için aynı tarihte atama var mı kontrol et
                var existingAssignment = await _context.VehicleShiftAssignments
                    .FirstOrDefaultAsync(vsa => vsa.ServiceVehicleID == assignmentDto.ServiceVehicleID && 
                                              vsa.AssignmentDate.Date == assignmentDto.AssignmentDate.Date);
                
                if (existingAssignment != null)
                {
                    return BadRequest(new { message = "Bu araç için bu tarihte zaten bir vardiya ataması bulunmaktadır." });
                }

                // Aynı vardiya için aynı tarihte başka araç ataması var mı kontrol et
                var existingShiftAssignment = await _context.VehicleShiftAssignments
                    .FirstOrDefaultAsync(vsa => vsa.ShiftID == assignmentDto.ShiftID && 
                                              vsa.AssignmentDate.Date == assignmentDto.AssignmentDate.Date);
                
                if (existingShiftAssignment != null)
                {
                    return BadRequest(new { message = "Bu vardiya için bu tarihte zaten bir araç ataması bulunmaktadır." });
                }

                var assignment = new VehicleShiftAssignment
                {
                    ServiceVehicleID = assignmentDto.ServiceVehicleID,
                    ShiftID = assignmentDto.ShiftID,
                    AssignmentDate = assignmentDto.AssignmentDate.Date, // Sadece tarih kısmını al
                    CreatedAt = DateTime.UtcNow
                };

                _context.VehicleShiftAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // Oluşturulan atamayı ilişkili verilerle birlikte getir
                var createdAssignment = await _context.VehicleShiftAssignments
                    .Include(vsa => vsa.ServiceVehicle)
                    .Include(vsa => vsa.Shift)
                    .FirstOrDefaultAsync(vsa => vsa.AssignmentID == assignment.AssignmentID);

                return CreatedAtAction(nameof(GetVehicleShiftAssignment), new { id = assignment.AssignmentID }, createdAssignment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-vardiya ataması oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/VehicleShiftAssignment/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult> CreateBulkAssignments(VehicleShiftAssignmentBulkCreateDto bulkDto)
        {
            try
            {
                var createdAssignments = new List<VehicleShiftAssignment>();
                var errors = new List<string>();

                foreach (var date in bulkDto.AssignmentDates)
                {
                    foreach (var vehicleShift in bulkDto.VehicleShiftPairs)
                    {
                        // Mevcut atama kontrolü
                        var existingAssignment = await _context.VehicleShiftAssignments
                            .FirstOrDefaultAsync(vsa => vsa.ServiceVehicleID == vehicleShift.ServiceVehicleID && 
                                                      vsa.ShiftID == vehicleShift.ShiftID &&
                                                      vsa.AssignmentDate.Date == date.Date);
                        
                        if (existingAssignment == null)
                        {
                            var assignment = new VehicleShiftAssignment
                            {
                                ServiceVehicleID = vehicleShift.ServiceVehicleID,
                                ShiftID = vehicleShift.ShiftID,
                                AssignmentDate = date.Date,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.VehicleShiftAssignments.Add(assignment);
                            createdAssignments.Add(assignment);
                        }
                        else
                        {
                            errors.Add($"Araç {vehicleShift.ServiceVehicleID} - Vardiya {vehicleShift.ShiftID} - Tarih {date:yyyy-MM-dd} için zaten atama mevcut.");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"{createdAssignments.Count} atama başarıyla oluşturuldu.",
                    createdCount = createdAssignments.Count,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Toplu atama oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/VehicleShiftAssignment/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicleShiftAssignment(int id, VehicleShiftAssignmentUpdateDto assignmentDto)
        {
            try
            {
                var assignment = await _context.VehicleShiftAssignments.FindAsync(id);
                if (assignment == null)
                {
                    return NotFound(new { message = "Araç-vardiya ataması bulunamadı." });
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

                // Shift kontrolü
                if (assignmentDto.ShiftID.HasValue)
                {
                    var shift = await _context.Shifts.FindAsync(assignmentDto.ShiftID.Value);
                    if (shift == null)
                    {
                        return BadRequest(new { message = "Geçersiz vardiya ID'si." });
                    }
                    assignment.ShiftID = assignmentDto.ShiftID.Value;
                }

                if (assignmentDto.AssignmentDate.HasValue)
                    assignment.AssignmentDate = assignmentDto.AssignmentDate.Value.Date;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Araç-vardiya ataması başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-vardiya ataması güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/VehicleShiftAssignment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicleShiftAssignment(int id)
        {
            try
            {
                var assignment = await _context.VehicleShiftAssignments.FindAsync(id);
                if (assignment == null)
                {
                    return NotFound(new { message = "Araç-vardiya ataması bulunamadı." });
                }

                // İlişkili tracking kayıtları var mı kontrol et
                var hasTrackings = await _context.Trackings
                    .AnyAsync(t => t.ServiceVehicleID == assignment.ServiceVehicleID && 
                                  t.ShiftID == assignment.ShiftID);
                
                if (hasTrackings)
                {
                    return BadRequest(new { message = "Bu atamaya bağlı takip kayıtları bulunduğu için silinemez." });
                }

                _context.VehicleShiftAssignments.Remove(assignment);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Araç-vardiya ataması başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Araç-vardiya ataması silinirken bir hata oluştu.", error = ex.Message });
            }
        }
    }

    // DTOs
    public class VehicleShiftAssignmentCreateDto
    {
        [Required]
        public int ServiceVehicleID { get; set; }

        [Required]
        public int ShiftID { get; set; }

        [Required]
        public DateTime AssignmentDate { get; set; }
    }

    public class VehicleShiftAssignmentUpdateDto
    {
        public int? ServiceVehicleID { get; set; }
        public int? ShiftID { get; set; }
        public DateTime? AssignmentDate { get; set; }
    }

    public class VehicleShiftAssignmentBulkCreateDto
    {
        [Required]
        public List<DateTime> AssignmentDates { get; set; } = new List<DateTime>();

        [Required]
        public List<VehicleShiftPair> VehicleShiftPairs { get; set; } = new List<VehicleShiftPair>();
    }

    public class VehicleShiftPair
    {
        [Required]
        public int ServiceVehicleID { get; set; }

        [Required]
        public int ShiftID { get; set; }
    }
}