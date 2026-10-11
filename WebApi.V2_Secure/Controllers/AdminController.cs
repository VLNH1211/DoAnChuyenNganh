using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.V2_Secure.Data;
using WebApi.V2_Secure.DTOs;
using WebApi.V2_Secure.Services;

namespace WebApi.V2_Secure.Controllers
{
    /// <summary>
    /// KB2 - Bản v2 (After): RBAC theo Role Claim với Policy "AdminOnly"
    /// Chỉ người dùng có Role = "Admin" mới được phép thực thi.
    /// NguoiDung (userA) hoặc NhanVien (staff1) gọi sẽ nhận 403 Forbidden và bị ghi Audit Log.
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly V2DbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            V2DbContext context,
            IAuditLogService auditLogService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        private (string Username, string Role) GetCurrentUserInfo()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value
                        ?? User.FindFirst("unique_name")?.Value
                        ?? "Admin";

            var role = User.FindFirst(ClaimTypes.Role)?.Value
                    ?? User.FindFirst("role")?.Value
                    ?? "Admin";

            return (username, role);
        }

        /// <summary>
        /// KB2 - Lấy danh sách toàn bộ người dùng hệ thống (Chỉ dành riêng cho Admin)
        /// Bảo mật: Đã ẩn toàn bộ PasswordHash, áp dụng RBAC Policy AdminOnly
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var (username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            await _auditLogService.LogAsync(
                username,
                role,
                "/api/admin/users",
                "GET",
                200,
                $"[KB2 Admin Truy Cập Hợp Lệ] Quản trị viên '{username}' xem danh sách {users.Count} người dùng.",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V2] Lấy danh sách người dùng thành công (RBAC Bảo mật: Đã kiểm tra quyền Admin, loại bỏ hoàn toàn mã băm mật khẩu).",
                total = users.Count,
                data = users
            });
        }

        /// <summary>
        /// API Xem Nhật Ký An Ninh (Audit Logs) hệ thống (Dành riêng cho Admin)
        /// Phục vụ giám sát các sự kiện: 401 Unauthorized, 403 Forbidden, 400 Validation/SQLi, 200 OK.
        /// </summary>
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int? statusCode, [FromQuery] int limit = 100)
        {
            var (username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            IQueryable<Models.AuditLog> query = _context.AuditLogs.AsNoTracking();

            if (statusCode.HasValue)
            {
                query = query.Where(l => l.StatusCode == statusCode.Value);
            }

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(Math.Min(limit, 500))
                .ToListAsync();

            await _auditLogService.LogAsync(
                username,
                role,
                "/api/admin/audit-logs",
                "GET",
                200,
                $"[AuditLog Truy Vấn] Admin '{username}' tra cứu {logs.Count} bản ghi nhật ký kiểm toán.",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V2] Truy vấn nhật ký an ninh (Audit Logs) thành công.",
                total = logs.Count,
                data = logs
            });
        }
    }
}
