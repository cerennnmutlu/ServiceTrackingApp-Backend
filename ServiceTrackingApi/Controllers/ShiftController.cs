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
    public class ShiftController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public ShiftController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/Shift
        [HttpGet]
        [Authorize(Roles = "Admin,ShiftManager,SecurityGuard")]
        public async Task<ActionResult<IEnumerable<Shift>>> GetShifts()
        {
            try
            {
                var shifts = await _context.Shifts.ToListAsync();
                return Ok(shifts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Vardiyalar getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Shift/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,ShiftManager,SecurityGuard")]
        public async Task<ActionResult<Shift>> GetShift(int id)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);

                if (shift == null)
                {
                    return NotFound(new { message = "Vardiya bulunamadı." });
                }

                return Ok(shift);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Vardiya getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/Shift
        [HttpPost]
        [Authorize(Roles = "Admin,ShiftManager")]
        public async Task<ActionResult<Shift>> CreateShift(ShiftCreateDto shiftDto)
        {
            try
            {
                // Vardiya adı kontrolü
                var existingShift = await _context.Shifts
                    .FirstOrDefaultAsync(s => s.ShiftName == shiftDto.ShiftName);
                
                if (existingShift != null)
                {
                    return BadRequest(new { message = "Bu vardiya adı zaten kayıtlı." });
                }

                // Zaman kontrolü
                if (shiftDto.StartTime >= shiftDto.EndTime)
                {
                    return BadRequest(new { message = "Başlangıç saati bitiş saatinden önce olmalıdır." });
                }

                // Çakışan vardiya kontrolü
                var overlappingShift = await _context.Shifts
                    .Where(s => s.Status == "Active")
                    .Where(s => 
                        (shiftDto.StartTime >= s.StartTime && shiftDto.StartTime < s.EndTime) ||
                        (shiftDto.EndTime > s.StartTime && shiftDto.EndTime <= s.EndTime) ||
                        (shiftDto.StartTime <= s.StartTime && shiftDto.EndTime >= s.EndTime)
                    )
                    .FirstOrDefaultAsync();

                if (overlappingShift != null)
                {
                    return BadRequest(new { message = $"Bu zaman aralığı '{overlappingShift.ShiftName}' vardiyası ile çakışıyor." });
                }

                var shift = new Shift
                {
                    ShiftName = shiftDto.ShiftName,
                    StartTime = shiftDto.StartTime,
                    EndTime = shiftDto.EndTime,
                    Status = shiftDto.Status ?? "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetShift), new { id = shift.ShiftID }, shift);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Vardiya oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/Shift/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ShiftManager")]
        public async Task<IActionResult> UpdateShift(int id, ShiftUpdateDto shiftDto)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);
                if (shift == null)
                {
                    return NotFound(new { message = "Vardiya bulunamadı." });
                }

                // Vardiya adı kontrolü (kendisi hariç)
                if (!string.IsNullOrEmpty(shiftDto.ShiftName) && shiftDto.ShiftName != shift.ShiftName)
                {
                    var existingShift = await _context.Shifts
                        .FirstOrDefaultAsync(s => s.ShiftName == shiftDto.ShiftName && s.ShiftID != id);
                    
                    if (existingShift != null)
                    {
                        return BadRequest(new { message = "Bu vardiya adı zaten kayıtlı." });
                    }
                }

                // Zaman kontrolü
                var startTime = shiftDto.StartTime ?? shift.StartTime;
                var endTime = shiftDto.EndTime ?? shift.EndTime;
                
                if (startTime >= endTime)
                {
                    return BadRequest(new { message = "Başlangıç saati bitiş saatinden önce olmalıdır." });
                }

                // Çakışan vardiya kontrolü (kendisi hariç)
                var overlappingShift = await _context.Shifts
                    .Where(s => s.Status == "Active" && s.ShiftID != id)
                    .Where(s => 
                        (startTime >= s.StartTime && startTime < s.EndTime) ||
                        (endTime > s.StartTime && endTime <= s.EndTime) ||
                        (startTime <= s.StartTime && endTime >= s.EndTime)
                    )
                    .FirstOrDefaultAsync();

                if (overlappingShift != null)
                {
                    return BadRequest(new { message = $"Bu zaman aralığı '{overlappingShift.ShiftName}' vardiyası ile çakışıyor." });
                }

                // Güncelleme
                if (!string.IsNullOrEmpty(shiftDto.ShiftName))
                    shift.ShiftName = shiftDto.ShiftName;
                
                if (shiftDto.StartTime.HasValue)
                    shift.StartTime = shiftDto.StartTime.Value;
                
                if (shiftDto.EndTime.HasValue)
                    shift.EndTime = shiftDto.EndTime.Value;
                
                if (!string.IsNullOrEmpty(shiftDto.Status))
                    shift.Status = shiftDto.Status;

                shift.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Vardiya başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Vardiya güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/Shift/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);
                if (shift == null)
                {
                    return NotFound(new { message = "Vardiya bulunamadı." });
                }

                // İlişkili kayıtları kontrol et
                var hasShiftAssignments = await _context.VehicleShiftAssignments
                    .AnyAsync(vsa => vsa.ShiftID == id);
                
                var hasTrackings = await _context.Trackings
                    .AnyAsync(t => t.ShiftID == id);

                if (hasShiftAssignments || hasTrackings)
                {
                    return BadRequest(new { message = "Bu vardiya başka kayıtlarda kullanıldığı için silinemez. Önce durumunu 'Inactive' olarak değiştirin." });
                }

                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Vardiya başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Vardiya silinirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Shift/active
        [HttpGet("active")]
        [Authorize(Roles = "Admin,ShiftManager,SecurityGuard")]
        public async Task<ActionResult<IEnumerable<Shift>>> GetActiveShifts()
        {
            try
            {
                var shifts = await _context.Shifts
                    .Where(s => s.Status == "Active")
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();

                return Ok(shifts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Aktif vardiyalar getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Shift/current
        [HttpGet("current")]
        [Authorize(Roles = "Admin,ShiftManager,SecurityGuard")]
        public async Task<ActionResult<Shift>> GetCurrentShift()
        {
            try
            {
                var currentTime = DateTime.Now.TimeOfDay;
                
                var currentShift = await _context.Shifts
                    .Where(s => s.Status == "Active")
                    .Where(s => s.StartTime <= currentTime && s.EndTime > currentTime)
                    .FirstOrDefaultAsync();

                if (currentShift == null)
                {
                    return NotFound(new { message = "Şu anda aktif bir vardiya bulunamadı." });
                }

                return Ok(currentShift);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Mevcut vardiya getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Shift/by-time/{time}
        [HttpGet("by-time/{time}")]
        public async Task<ActionResult<Shift>> GetShiftByTime(string time)
        {
            try
            {
                if (!TimeSpan.TryParse(time, out TimeSpan timeSpan))
                {
                    return BadRequest(new { message = "Geçersiz zaman formatı. HH:mm formatında girin." });
                }
                
                var shift = await _context.Shifts
                    .Where(s => s.Status == "Active")
                    .Where(s => s.StartTime <= timeSpan && s.EndTime > timeSpan)
                    .FirstOrDefaultAsync();

                if (shift == null)
                {
                    return NotFound(new { message = "Belirtilen zamanda aktif bir vardiya bulunamadı." });
                }

                return Ok(shift);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Vardiya getirilirken bir hata oluştu.", error = ex.Message });
            }
        }
    }

    // DTO Classes
    public class ShiftCreateDto
    {
        public string ShiftName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Status { get; set; }
    }

    public class ShiftUpdateDto
    {
        public string? ShiftName { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? Status { get; set; }
    }
}