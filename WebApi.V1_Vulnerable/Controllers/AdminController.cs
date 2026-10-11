using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.V1_Vulnerable.Data;

namespace WebApi.V1_Vulnerable.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly V1DbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(V1DbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// KB2 - Bản v1 (Before): Người dùng thường gọi API quản trị
        /// KHÔNG kiểm tra vai trò (Role). Bất kỳ ai (Anonymous, userA, staff1) gọi vẫn nhận 200 OK.
        /// Lộ toàn bộ danh sách người dùng và mật khẩu plaintext.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            _logger.LogInformation("[V1 - KB2] GetAllUsers: API quản trị bị truy cập công khai (Thiếu RBAC Role Check)");

            var users = await _context.Users
                .OrderBy(u => u.Id)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "[V1] Lấy danh sách toàn bộ người dùng thành công (Lỗ hổng phân quyền KB2: Bất kỳ vai trò nào cũng gọi được API Quản trị).",
                total = users.Count,
                data = users
            });
        }

        /// <summary>
        /// API Xem Audit Logs (V1: Không kiểm soát quyền)
        /// </summary>
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                total = logs.Count,
                data = logs
            });
        }
    }
}
