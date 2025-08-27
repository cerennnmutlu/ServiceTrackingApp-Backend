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
    public class RoleController : ControllerBase
    {
        private readonly RepositoryContext _context;

        public RoleController(RepositoryContext context)
        {
            _context = context;
        }

        // GET: api/Role
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .Include(r => r.Users)
                    .ToListAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Roller getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Role/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.Users)
                    .FirstOrDefaultAsync(r => r.RoleID == id);

                if (role == null)
                {
                    return NotFound(new { message = "Rol bulunamadı." });
                }

                return Ok(role);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Rol getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // POST: api/Role
        [HttpPost]
        public async Task<ActionResult<Role>> CreateRole(RoleCreateDto roleDto)
        {
            try
            {
                // Rol adı kontrolü
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == roleDto.RoleName);
                
                if (existingRole != null)
                {
                    return BadRequest(new { message = "Bu rol adı zaten kayıtlı." });
                }

                var role = new Role
                {
                    RoleName = roleDto.RoleName
                };

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRole), new { id = role.RoleID }, role);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Rol oluşturulurken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/Role/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, RoleUpdateDto roleDto)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Rol bulunamadı." });
                }

                // Rol adı kontrolü (kendisi hariç)
                if (!string.IsNullOrEmpty(roleDto.RoleName) && roleDto.RoleName != role.RoleName)
                {
                    var existingRole = await _context.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == roleDto.RoleName && r.RoleID != id);
                    
                    if (existingRole != null)
                    {
                        return BadRequest(new { message = "Bu rol adı zaten kayıtlı." });
                    }
                }

                // Güncelleme
                if (!string.IsNullOrEmpty(roleDto.RoleName))
                    role.RoleName = roleDto.RoleName;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Rol başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Rol güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

        // DELETE: api/Role/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Rol bulunamadı." });
                }

                // İlişkili kullanıcıları kontrol et
                var hasUsers = await _context.Users
                    .AnyAsync(u => u.RoleID == id);

                if (hasUsers)
                {
                    return BadRequest(new { message = "Bu rol kullanıcılar tarafından kullanıldığı için silinemez." });
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Rol başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Rol silinirken bir hata oluştu.", error = ex.Message });
            }
        }
    }

    // DTO 
    public class RoleCreateDto
    {
        public string RoleName { get; set; } = string.Empty;
    }

    public class RoleUpdateDto
    {
        public string? RoleName { get; set; }
    }
}