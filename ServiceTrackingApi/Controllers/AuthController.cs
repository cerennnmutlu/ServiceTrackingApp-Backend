using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Data;
using ServiceTrackingApi.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ServiceTrackingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RepositoryContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(RepositoryContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required");
            }

            // Find user by username or email
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

            if (user == null)
            {
                return Unauthorized("Invalid credentials");
            }

            // Verify password
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials");
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                User = new UserInfo
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    RoleName = user.Role.RoleName
                },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] RegisterRequest request)
        {
            // Validate input
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) ||
                string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.FullName))
            {
                return BadRequest("All fields are required");
            }

            // Check if username already exists
            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == request.Username || u.Email == request.Email);
            if (existingUser)
            {
                return BadRequest("Username or email already exists");
            }

            // Check if role exists
            var roleExists = await _context.Roles.AnyAsync(r => r.RoleID == request.RoleID);
            if (!roleExists)
            {
                return BadRequest("Invalid role");
            }

            // Hash password
            var passwordHash = HashPassword(request.Password);

            var user = new User
            {
                FullName = request.FullName,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleID = request.RoleID,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Remove password hash from response
            user.PasswordHash = string.Empty;

            return CreatedAtAction("GetUser", "User", new { id = user.UserID }, user);
        }

        // POST: api/Auth/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserID);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Verify current password
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Current password is incorrect");
            }

            // Hash new password
            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok("Password changed successfully");
        }

        // GET: api/Auth/roles
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        // GET: api/Auth/validate-token
        [HttpGet("validate-token")]
        public IActionResult ValidateToken()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("Token is required");
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-here");
                
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "userId").Value);
                var username = jwtToken.Claims.First(x => x.Type == "username").Value;
                var role = jwtToken.Claims.First(x => x.Type == "role").Value;

                return Ok(new
                {
                    Valid = true,
                    UserID = userId,
                    Username = username,
                    Role = role
                });
            }
            catch
            {
                return Unauthorized("Invalid token");
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "your-secret-key-here");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", user.UserID.ToString()),
                    new Claim("username", user.Username),
                    new Claim("email", user.Email),
                    new Claim("role", user.Role.RoleName)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // Request/Response Models
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserInfo User { get; set; } = new UserInfo();
        public DateTime ExpiresAt { get; set; }
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