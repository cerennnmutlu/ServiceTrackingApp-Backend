using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Data;
using ServiceTrackingApi.Models;
using ServiceTrackingApi.Security; // PBKDF2 hasher
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ServiceTrackingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController] // Otomatik model state doğrulama, 400 bad request vs. için yardımcı
    public class AuthController : ControllerBase
    {
        private readonly RepositoryContext _context;        // EF Core DbContext (Users, Roles tablosu)
        private readonly IConfiguration _configuration;     // appsettings.json’dan Jwt:Key, Issuer, Audience vs. okumak için

        public AuthController(RepositoryContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required");

            
            var user = await _context.Users
                .Include(u => u.Role) // Token'a role claim eklemek için Role ilişkisini çekiyoruz
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

            if (user is null)
                return Unauthorized("Invalid credentials");

            // PBKDF2 ile parolayı doğrula
            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            // JWT üret
            var token = GenerateJwtToken(user, out DateTime expiresAtUtc);

            return Ok(new LoginResponse
            {
                Token = token,
                User = new UserInfo
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    RoleName = user.Role?.RoleName ?? string.Empty
                },
                ExpiresAt = expiresAtUtc
            });
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] RegisterRequest request)
        {
            // Basit doğrulamalar
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest("All fields are required");
            }

            // Şifre uzunluk kontrolü
            if (request.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters long");

            // Email format kontrolü (basit)
            if (!request.Email.Contains("@"))
                return BadRequest("Invalid email format");

            // Kullanıcı/Email var mı?
            bool exists = await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email);
            if (exists) return BadRequest("Username or email already exists");

            // Rol var mı?
            bool roleExists = await _context.Roles.AnyAsync(r => r.RoleID == request.RoleID);
            if (!roleExists) return BadRequest("Invalid role");

            // Parola hashle (PBKDF2)
            string hash = PasswordHasher.Hash(request.Password);

            var user = new User
            {
                FullName = request.FullName,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hash,
                RoleID = request.RoleID,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Güvenlik gereği response'ta hash'i göndermiyoruz
            user.PasswordHash = string.Empty;

            // Başarılı kayıt sonrası kullanıcı bilgilerini döndür
            return Ok(new { message = "User registered successfully", user = new UserInfo
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                RoleName = "" // Rol bilgisi için ayrı sorgu gerekir
            }});
        }

        // POST: api/Auth/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Current password and new password are required");

            if (request.NewPassword.Length < 6)
                return BadRequest("New password must be at least 6 characters long");

            var user = await _context.Users.FindAsync(request.UserID);
            if (user is null) return NotFound("User not found");

            
            if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest("Current password is incorrect");

            // Yeni parolayı hashle ve kaydet
            user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        // GET: api/Auth/roles  -Roller listesi 
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
            => await _context.Roles.ToListAsync();

        // GET: api/Auth/validate-token
        // Header: Authorization: Bearer <token>
        [HttpGet("validate-token")]
        public IActionResult ValidateToken()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').Last();
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized("Token is required");

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "dev-key-change-me");
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                // Üretimle aynı doğrulama parametreleri (Issuer/Audience/Lifetime/Key)
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateIssuer = !string.IsNullOrEmpty(issuer),
                    ValidIssuer = issuer,

                    ValidateAudience = !string.IsNullOrEmpty(audience),
                    ValidAudience = audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // expire olmuş token geçmesin
                }, out var validated);

                var jwt = (JwtSecurityToken)validated;

                
                var userId = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                             ?? jwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value; 
                var username = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                               ?? jwt.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
                var role = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                           ?? jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                return Ok(new { Valid = true, UserID = userId, Username = username, Role = role });
            }
            catch
            {
                return Unauthorized("Invalid token");
            }
        }

        private string GenerateJwtToken(User user, out DateTime expiresAtUtc)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "dev-key-change-me");
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var claims = new List<Claim>
            {
                
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),

                
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),

               
                new Claim("userId", user.UserID.ToString()),
                new Claim("username", user.Username ?? string.Empty),
                new Claim("email", user.Email ?? string.Empty)
            };

            // Rol claim'i: [Authorize(Roles="Admin")] ile çalışması için ClaimTypes.Role kullan
            if (!string.IsNullOrEmpty(user.Role?.RoleName))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleName));
                claims.Add(new Claim("role", user.Role.RoleName));
            }

            var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;
            var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryInHours", 24);
            expiresAtUtc = now.AddHours(expiryHours);

            var token = new JwtSecurityToken(
                issuer: issuer,              
                audience: audience,          
                claims: claims,
                notBefore: now,
                expires: expiresAtUtc,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // --- Request/Response DTO'lar ---

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty; // kullanıcı adı veya email
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;    // Erişim token'ı (Bearer)
        public UserInfo User { get; set; } = new UserInfo(); // UI için temel kullanıcı bilgisi
        public DateTime ExpiresAt { get; set; }              // Token bitiş zamanı (UTC)
    }

    public class UserInfo
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleID { get; set; }
    }

    public class ChangePasswordRequest
    {
        public int UserID { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
