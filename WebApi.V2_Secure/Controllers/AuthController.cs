using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApi.V2_Secure.Data;
using WebApi.V2_Secure.DTOs;
using WebApi.V2_Secure.Services;

namespace WebApi.V2_Secure.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly V2DbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            V2DbContext context,
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// API Đăng nhập hệ thống (V2: Bảo mật cao - Kiểm tra mật khẩu bằng BCrypt, cấp JWT Bearer)
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Dữ liệu đầu vào không hợp lệ."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // Kiểm tra user tồn tại và kiểm tra hash BCrypt
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                await _auditLogService.LogAsync(
                    request.Username,
                    "None",
                    "/api/auth/login",
                    "POST",
                    401,
                    $"[Đăng nhập thất bại] Sai thông tin xác thực cho tài khoản: '{request.Username}'",
                    ip);

                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc mật khẩu không chính xác."
                });
            }

            // Sinh JWT Bearer Token chuẩn RFC 7519
            var secretKey = _configuration["JwtSettings:SecretKey"] 
                ?? "SuperSecretSecureKeyForHutechDoAnChuyenNganhSecurityLab20261010!";
            var issuer = _configuration["JwtSettings:Issuer"] ?? "SecurityLab_V2";
            var audience = _configuration["JwtSettings:Audience"] ?? "SecurityLab_Client";
            var durationMinutes = int.Parse(_configuration["JwtSettings:DurationInMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var expiresAt = DateTime.UtcNow.AddMinutes(durationMinutes);
            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            await _auditLogService.LogAsync(
                user.Username,
                user.Role,
                "/api/auth/login",
                "POST",
                200,
                $"[Đăng nhập thành công] Cấp JWT Token thành công cho User '{user.Username}' (Role: {user.Role})",
                ip);

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Đăng nhập thành công (Phiên bản V2 - Bảo mật).",
                Token = token,
                ExpiresInSeconds = durationMinutes * 60,
                Username = user.Username,
                Role = user.Role,
                FullName = user.FullName
            });
        }
    }
}
