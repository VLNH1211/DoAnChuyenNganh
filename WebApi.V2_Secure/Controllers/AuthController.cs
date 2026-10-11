using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IRateLimitingService _rateLimitingService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            V2DbContext context,
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ITokenBlacklistService tokenBlacklistService,
            IRateLimitingService rateLimitingService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogService = auditLogService;
            _tokenBlacklistService = tokenBlacklistService;
            _rateLimitingService = rateLimitingService;
            _logger = logger;
        }

        /// <summary>
        /// API Đăng nhập hệ thống (V2: Bảo mật cao - BCrypt verify, Anti-Brute-Force & Cấp JWT Bearer có claim JTI)
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // 1. Kiểm tra Anti-Brute-Force & Rate Limiting
            if (_rateLimitingService.IsLoginBlocked(ip, request.Username, out int retryAfter))
            {
                Response.Headers["Retry-After"] = retryAfter.ToString();

                await _auditLogService.LogAsync(
                    request.Username,
                    "None",
                    "/api/auth/login",
                    "POST",
                    429,
                    $"[Brute-Force Bị Chặn] Tài khoản '{request.Username}' bị khóa tạm thời do nhập sai quá 5 lần. Vui lòng thử lại sau {retryAfter} giây.",
                    ip);

                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    success = false,
                    statusCode = 429,
                    message = $"[V2 - Chặn Brute-Force] Quá nhiều lần đăng nhập không thành công. Tài khoản tạm thời bị khóa. Vui lòng thử lại sau {retryAfter} giây.",
                    error = "TooManyRequests_BruteForceBlocked",
                    retryAfterSeconds = retryAfter
                });
            }

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

            // 2. Kiểm tra user tồn tại và kiểm tra hash BCrypt
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Ghi nhận lần nhập sai cho bộ đếm Brute-force
                _rateLimitingService.RecordFailedLogin(ip, request.Username);

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

            // Đăng nhập thành công -> Reset bộ đếm lỗi
            _rateLimitingService.ResetFailedLogin(ip, request.Username);

            // 3. Sinh JWT Bearer Token chuẩn RFC 7519 kèm JTI (Unique Token ID)
            var secretKey = _configuration["JwtSettings:SecretKey"] 
                ?? "SuperSecretSecureKeyForHutechDoAnChuyenNganhSecurityLab20261010!";
            var issuer = _configuration["JwtSettings:Issuer"] ?? "SecurityLab_V2";
            var audience = _configuration["JwtSettings:Audience"] ?? "SecurityLab_Client";
            var durationMinutes = int.Parse(_configuration["JwtSettings:DurationInMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jti = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
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
                $"[Đăng nhập thành công] Cấp JWT Token (JTI: {jti}) cho '{user.Username}' (Role: {user.Role}) qua xác thực BCrypt.",
                ip);

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Đăng nhập thành công (Phiên bản V2 - Bảo mật BCrypt + JWT JTI).",
                Token = token,
                ExpiresInSeconds = durationMinutes * 60,
                Username = user.Username,
                Role = user.Role,
                FullName = user.FullName
            });
        }

        /// <summary>
        /// API Đăng xuất & Thu hồi Token (V2: Token Revocation / JTI Blacklist)
        /// Thêm JTI của token vào Blacklist để vô hiệu hóa ngay lập tức.
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "User";
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "NguoiDung";
            var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value 
                   ?? User.FindFirst("jti")?.Value 
                   ?? "";

            DateTime expiry = DateTime.UtcNow.AddMinutes(60);
            var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
            if (long.TryParse(expClaim, out var expUnix))
            {
                expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            }

            if (!string.IsNullOrEmpty(jti))
            {
                _tokenBlacklistService.RevokeToken(jti, expiry);
            }

            await _auditLogService.LogAsync(
                username,
                role,
                "/api/auth/logout",
                "POST",
                200,
                $"[Đăng xuất & Thu hồi Token] Thu hồi JWT Token thành công (JTI: '{jti}'). Token đã được đưa vào JTI Blacklist.",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V2 - Đăng xuất An toàn] Thu hồi Token thành công. Token (JTI) đã được thêm vào Blacklist và không còn giá trị sử dụng.",
                revokedJti = jti
            });
        }

        /// <summary>
        /// API Lấy thông tin tài khoản hiện tại (Kiểm tra token còn hợp lệ hay đã bị thu hồi)
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("userId")?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? User.FindFirst("jti")?.Value;

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V2] Token hợp lệ và phiên làm việc đang hoạt động.",
                data = new
                {
                    userId,
                    username,
                    role,
                    jti
                }
            });
        }
    }
}
