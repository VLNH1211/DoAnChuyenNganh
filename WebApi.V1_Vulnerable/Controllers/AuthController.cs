using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApi.V1_Vulnerable.Data;
using WebApi.V1_Vulnerable.DTOs;

namespace WebApi.V1_Vulnerable.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly V1DbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(V1DbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// API Đăng nhập hệ thống (V1: Baseline - So sánh mật khẩu trực tiếp)
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Tên đăng nhập và mật khẩu không được để trống."
                });
            }

            // V1: So sánh plaintext password trực tiếp trong CSDL
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Tên đăng nhập hoặc mật khẩu không chính xác."
                });
            }

            // Tạo JWT Bearer Token
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? "VulnerableSecretKeyForDemoPurposeOnlyNotSecure1234567890";
            var issuer = _configuration["JwtSettings:Issuer"] ?? "SecurityLab_V1";
            var audience = _configuration["JwtSettings:Audience"] ?? "SecurityLab_Client";
            var durationMinutes = int.Parse(_configuration["JwtSettings:DurationInMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(durationMinutes),
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Đăng nhập thành công (Phiên bản V1 - Baseline).",
                Token = token,
                Username = user.Username,
                Role = user.Role,
                FullName = user.FullName
            });
        }
    }
}
