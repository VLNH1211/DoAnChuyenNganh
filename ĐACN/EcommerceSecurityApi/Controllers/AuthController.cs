using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcommerceSecurityApi.DTOs.Auth;
using EcommerceSecurityApi.Helpers;
using EcommerceSecurityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EcommerceSecurityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly ISecurityLogService _securityLogService;

    public AuthController(
        IAuthService authService,
        ITokenBlacklistService tokenBlacklistService,
        ISecurityLogService securityLogService)
    {
        _authService = authService;
        _tokenBlacklistService = tokenBlacklistService;
        _securityLogService = securityLogService;
    }

    /// <summary>
    /// API Đăng nhập hệ thống (Cấp phát mã JWT Token - Chống Brute-force bằng Rate Limiting 5 lần/phút)
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("LoginLimit")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var (success, message, data) = await _authService.LoginAsync(request);

        if (!success)
        {
            return Unauthorized(new
            {
                success = false,
                message = message,
                statusCode = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(data);
    }

    /// <summary>
    /// API Đăng ký tài khoản dành cho Khách hàng
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var (success, message, data) = await _authService.RegisterCustomerAsync(request);

        if (!success)
        {
            return BadRequest(new
            {
                success = false,
                message = message,
                statusCode = StatusCodes.Status400BadRequest
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = message,
            data = data
        });
    }

    /// <summary>
    /// API Xác thực danh tính người dùng hiện tại (Bắt buộc có Bearer Token)
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Không thể trích xuất danh tính từ Token." });
        }

        var userInfo = await _authService.GetCurrentUserAsync(userId);
        if (userInfo == null)
        {
            return NotFound(new { success = false, message = "Người dùng không tồn tại." });
        }

        return Ok(new
        {
            success = true,
            message = "Xác thực danh tính thành công qua JWT Claims.",
            user = userInfo
        });
    }

    /// <summary>
    /// API Đăng xuất - Thu hồi mã JWT Token hiện tại (Chống Replay Attack & Chiếm quyền phiên - OWASP API2:2023)
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                  ?? User.FindFirst("jti")?.Value;

        DateTime? expiresAt = null;
        var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value
                       ?? User.FindFirst("exp")?.Value;
        if (long.TryParse(expClaim, out long expSeconds))
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
        }

        if (!string.IsNullOrEmpty(jti))
        {
            _tokenBlacklistService.RevokeToken(jti, expiresAt);
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out int uid) ? uid : null;
        var username = User.Identity?.Name ?? (userId.HasValue ? $"User_{userId}" : "Anonymous");
        var clientIp = ClientInfoHelper.GetClientIpAddress(HttpContext);

        await _securityLogService.LogIncidentAsync(
            incidentType: "USER_LOGOUT",
            severity: "Info",
            description: $"Người dùng '{username}' (ID: {userId}) đã thực hiện đăng xuất an toàn. Mã JWT Token (JTI: {jti}) đã được đưa vào danh sách thu hồi (Blacklist).",
            targetResource: "POST /api/auth/logout",
            userId: userId,
            username: username,
            ipAddress: clientIp
        );

        return Ok(new
        {
            success = true,
            statusCode = StatusCodes.Status200OK,
            message = "Đăng xuất thành công! Token xác thực của bạn đã bị thu hồi an toàn.",
            revokedJti = jti,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// API Kiểm tra phân quyền: Chỉ Quản trị viên (Admin) mới có quyền truy cập
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            success = true,
            message = "Chào mừng Admin! Bạn có toàn quyền quản trị hệ thống.",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// API Kiểm tra phân quyền: Quản trị viên hoặc Nhân viên mới có quyền truy cập
    /// </summary>
    [Authorize(Roles = "Admin,NhanVien")]
    [HttpGet("staff-only")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult StaffOnly()
    {
        return Ok(new
        {
            success = true,
            message = "Truy cập thành công: Khu vực quản lý đơn hàng dành cho Nhân viên và Quản trị viên.",
            timestamp = DateTime.UtcNow
        });
    }
}
