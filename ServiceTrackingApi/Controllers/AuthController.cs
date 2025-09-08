using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingApi.Data;
using ServiceTrackingApi.Models;
using ServiceTrackingApi.Security; // PBKDF2 hasher
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            var tokenResponse = GenerateTokens(user);
            await _context.SaveChangesAsync(); // Refresh token'ı kaydet

            return Ok(tokenResponse);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest("Refresh token is required");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            if (user == null)
                return Unauthorized("Invalid refresh token");

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                // Süresi dolmuş refresh token'ı temizle
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
                return Unauthorized("Refresh token has expired");
            }

            var tokenResponse = GenerateTokens(user);
            await _context.SaveChangesAsync();

            return Ok(tokenResponse);
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
                return BadRequest("Tüm alanlar gereklidir.");
            }

            // Şifre güçlülük kontrolü
            var passwordValidation = ValidatePassword(request.Password);
            if (!passwordValidation.IsValid)
                return BadRequest(passwordValidation.ErrorMessage);

            if (!IsValidEmail(request.Email))
                return BadRequest("Geçerli bir e-posta adresi giriniz.");

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
            return Ok(new
            {
                message = "User registered successfully",
                user = new UserInfo
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    RoleName = "" // Rol bilgisi için ayrı sorgu gerekir
                }
            });
        }

        // PUT: api/Auth/change-password - Kullanıcının kendi şifresini değiştir
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // JWT token'dan kullanıcı ID'sini al
                var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid token");
                }

                // Giriş doğrulaması
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                    return BadRequest("Mevcut şifre gereklidir.");

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                    return BadRequest("Yeni şifre gereklidir.");

                // Şifre güçlülük kontrolü
                var passwordValidation = ValidatePassword(request.NewPassword);
                if (!passwordValidation.IsValid)
                    return BadRequest(passwordValidation.ErrorMessage);

                // Kullanıcıyı veritabanından getir
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Mevcut şifre doğrulaması
                if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest("Mevcut şifre yanlış.");
                }

                // Yeni şifrenin mevcut şifreyle aynı olmaması kontrolü
                if (PasswordHasher.Verify(request.NewPassword, user.PasswordHash))
                {
                    return BadRequest("Yeni şifre mevcut şifreyle aynı olamaz.");
                }

                // Yeni şifreyi hashle ve kaydet
                user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Şifre başarıyla değiştirildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şifre değiştirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // GET: api/Auth/roles  -Roller listesi 
        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
            => await _context.Roles.ToListAsync();

        // GET: api/Auth/profile - Kullanıcının kendi profil bilgileri
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetProfile()
        {
            try
            {
                // JWT token'dan kullanıcı ID'sini al
                var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid token");
                }

                // Kullanıcı bilgilerini veritabanından getir
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Kullanıcı bilgilerini döndür
                return Ok(new UserInfo
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    RoleName = user.Role?.RoleName ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Profil bilgileri getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        // PUT: api/Auth/profile - Kullanıcının kendi profil bilgilerini güncelle
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                // JWT token'dan kullanıcı ID'sini al
                var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid token");
                }

                // Giriş doğrulaması
                if (string.IsNullOrWhiteSpace(request.FullName))
                    return BadRequest("Ad Soyad gereklidir.");

                if (string.IsNullOrWhiteSpace(request.Username))
                    return BadRequest("Kullanıcı adı gereklidir.");

                if (string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest("E-posta gereklidir.");

                // E-posta format kontrolü
                if (!IsValidEmail(request.Email))
                    return BadRequest("Geçerli bir e-posta adresi giriniz.");

                // Kullanıcıyı veritabanından getir
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Kullanıcı adı ve e-posta benzersizlik kontrolü (kendi kaydı hariç)
                var existingUserByUsername = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username && u.UserID != userId);
                if (existingUserByUsername != null)
                {
                    return BadRequest("Bu kullanıcı adı zaten kullanılıyor.");
                }

                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.UserID != userId);
                if (existingUserByEmail != null)
                {
                    return BadRequest("Bu e-posta adresi zaten kullanılıyor.");
                }

                // Kullanıcı bilgilerini güncelle
                user.FullName = request.FullName.Trim();
                user.Username = request.Username.Trim();
                user.Email = request.Email.Trim();

                await _context.SaveChangesAsync();

                // Güncellenmiş kullanıcı bilgilerini döndür
                var updatedUser = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                return Ok(new UserInfo
                {
                    UserID = updatedUser.UserID,
                    FullName = updatedUser.FullName,
                    Username = updatedUser.Username,
                    Email = updatedUser.Email,
                    RoleName = updatedUser.Role?.RoleName ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Profil güncellenirken bir hata oluştu.", error = ex.Message });
            }
        }

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

        private TokenResponse GenerateTokens(User user)
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
            };

            if (!string.IsNullOrEmpty(user.Role?.RoleName))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.RoleName));
                claims.Add(new Claim("role", user.Role.RoleName));
            }

            var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;

            // Access Token ayarları
            var accessTokenExpiryHours = _configuration.GetValue<int>("Jwt:AccessTokenExpiryInHours", 1);
            var accessTokenExpiresAt = now.AddHours(accessTokenExpiryHours);

            var accessToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: accessTokenExpiresAt,
                signingCredentials: credentials
            );

            // Refresh Token ayarları
            var refreshTokenExpiryDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpiryInDays", 7);
            var refreshTokenExpiresAt = now.AddDays(refreshTokenExpiryDays);
            var refreshToken = GenerateRefreshToken();

            // Kullanıcının refresh token bilgilerini güncelle
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = refreshTokenExpiresAt;

            return new TokenResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                User = new UserInfo
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    RoleName = user.Role?.RoleName ?? string.Empty
                }
            };
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "dev-key-change-me")),
                ValidateIssuer = !string.IsNullOrEmpty(_configuration["Jwt:Issuer"]),
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = !string.IsNullOrEmpty(_configuration["Jwt:Audience"]),
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = false // Süresi dolmuş token'ı doğrulamak için
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private (bool IsValid, string ErrorMessage) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Şifre boş olamaz.");

            if (password.Length < 8)
                return (false, "Şifre en az 8 karakter olmalıdır.");

            if (!password.Any(char.IsUpper))
                return (false, "Şifre en az bir büyük harf içermelidir.");

            if (!password.Any(char.IsLower))
                return (false, "Şifre en az bir küçük harf içermelidir.");

            if (!password.Any(char.IsDigit))
                return (false, "Şifre en az bir rakam içermelidir.");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                return (false, "Şifre en az bir özel karakter içermelidir.");

            return (true, string.Empty);
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
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public UserInfo User { get; set; } = new UserInfo();
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
