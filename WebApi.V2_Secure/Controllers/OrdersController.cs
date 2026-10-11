using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.V2_Secure.Data;
using WebApi.V2_Secure.DTOs;
using WebApi.V2_Secure.Services;

namespace WebApi.V2_Secure.Controllers
{
    [Authorize] // Bắt buộc đăng nhập với mọi endpoint trong Controller (KB1 Defense)
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly V2DbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            V2DbContext context,
            IAuditLogService auditLogService,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        private (int UserId, string Username, string Role) GetCurrentUserInfo()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("userId")?.Value
                         ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                         ?? "0";

            int.TryParse(userIdStr, out var userId);

            var username = User.FindFirst(ClaimTypes.Name)?.Value
                        ?? User.FindFirst("unique_name")?.Value
                        ?? "Unknown";

            var role = User.FindFirst(ClaimTypes.Role)?.Value
                    ?? User.FindFirst("role")?.Value
                    ?? "NguoiDung";

            return (userId, username, role);
        }

        /// <summary>
        /// KB1 - Bản v2 (After): Áp dụng JWT Bearer Authentication & Phân định phạm vi dữ liệu
        /// Yêu cầu Token hợp lệ. Nếu không có token -> 401 Unauthorized.
        /// Admin/Staff xem toàn bộ; NguoiDung chỉ xem đơn hàng của chính mình.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var (userId, username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            IQueryable<Models.Order> query = _context.Orders.AsNoTracking();

            // Phân định phạm vi dữ liệu an toàn theo vai trò
            if (role != "Admin" && role != "NhanVien")
            {
                query = query.Where(o => o.UserId == userId);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            await _auditLogService.LogAsync(
                username,
                role,
                "/api/orders",
                "GET",
                200,
                $"[KB1 Hợp lệ] User '{username}' (Role: {role}) truy xuất danh sách {orders.Count} đơn hàng thành công.",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V2] Lấy danh sách đơn hàng thành công (Đã xác thực JWT & Phân tách phạm vi dữ liệu).",
                userContext = new { username, role },
                total = orders.Count,
                data = orders
            });
        }

        /// <summary>
        /// KB3 - Bản v2 (After): Phòng chống BOLA/IDOR (Kiểm soát truy cập mức đối tượng)
        /// So sánh UserId chủ sở hữu đơn hàng với UserId trong JWT Claims.
        /// Nếu userA xem đơn của userB -> Trả về 403 Forbidden và ghi Audit Log.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var (userId, username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    statusCode = 404,
                    message = $"Không tìm thấy đơn hàng có mã ID = {id}."
                });
            }

            // KIỂM TRA CHỦ SỞ HỮU (Chống IDOR / BOLA)
            // Chỉ chủ sở hữu (UserId trùng khớp) hoặc Admin mới được phép xem đơn hàng
            if (order.UserId != userId && role != "Admin")
            {
                // Ghi nhận sự kiện vi phạm an ninh vào Audit Log
                await _auditLogService.LogAsync(
                    username,
                    role,
                    $"/api/orders/{id}",
                    "GET",
                    403,
                    $"[KB3 Chặn IDOR Thành Công] Người dùng '{username}' (Id: {userId}, Role: {role}) cố gắng truy cập trái phép đơn hàng #{id} thuộc về UserId {order.UserId}.",
                    ip);

                return StatusCode(403, new
                {
                    success = false,
                    statusCode = 403,
                    message = "[V2 - KB3 Chặn BOLA/IDOR] Truy cập bị từ chối: Bạn không phải là chủ sở hữu của đơn hàng này và không có quyền xem thông tin.",
                    error = "Forbidden_ObjectAccessDenied",
                    attemptedOrderId = id,
                    callerUserId = userId
                });
            }

            // Truy cập hợp lệ
            await _auditLogService.LogAsync(
                username,
                role,
                $"/api/orders/{id}",
                "GET",
                200,
                $"[KB3 Hợp Lệ] User '{username}' (Role: {role}) truy cập hợp lệ đơn hàng #{id}.",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = $"[V2] Lấy thông tin đơn hàng #{id} thành công (Đã kiểm tra quyền sở hữu đối tượng hợp lệ).",
                data = order
            });
        }

        /// <summary>
        /// KB4 - Bản v2 (After): Tìm kiếm đơn hàng - Kiểm tra nghiêm ngặt đầu vào & Truy vấn Parameterized LINQ
        /// Từ chối các ký tự lạ / mẫu SQLi (400 Bad Request), không bao giờ lỗi 500, bảo vệ CSDL nguyên vẹn.
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchOrders([FromQuery] string? keyword)
        {
            var (userId, username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // 1. Kiểm tra không được rỗng
            if (string.IsNullOrWhiteSpace(keyword))
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    "/api/orders/search",
                    "GET",
                    400,
                    "[KB4 Lỗi Validation] Từ khóa tìm kiếm rỗng.",
                    ip);

                return BadRequest(new
                {
                    success = false,
                    statusCode = 400,
                    message = "[V2 - KB4 Validation] Từ khóa tìm kiếm không được để trống.",
                    error = "KeywordRequired"
                });
            }

            var trimmedKeyword = keyword.Trim();

            // 2. Kiểm tra độ dài tối đa 30 ký tự
            if (trimmedKeyword.Length > 30)
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    "/api/orders/search",
                    "GET",
                    400,
                    $"[KB4 Lỗi Validation] Độ dài từ khóa vượt quá 30 ký tự: ({trimmedKeyword.Length} ký tự)",
                    ip);

                return BadRequest(new
                {
                    success = false,
                    statusCode = 400,
                    message = "[V2 - KB4 Validation] Độ dài từ khóa tìm kiếm tối đa 30 ký tự.",
                    error = "KeywordTooLong"
                });
            }

            // 3. Kiểm tra ký tự nguy hiểm / Pattern SQL Injection
            // Danh sách ký tự/từ khóa cấm: ', ", ;, --, /*, */, xp_, EXEC, UNION, OR 1=1, DROP, SELECT, INSERT
            string[] dangerousPatterns = { "'", "\"", ";", "--", "/*", "*/", "xp_", "exec", "union", "or 1=1", "drop ", "alter ", "delete ", "insert " };
            bool hasSqlInjectionPattern = dangerousPatterns.Any(p => trimmedKeyword.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);

            // Cho phép ký tự chữ cái (hỗ trợ Unicode tiếng Việt), chữ số, khoảng trắng và dấu gạch ngang
            bool isValidCharacters = Regex.IsMatch(trimmedKeyword, @"^[\p{L}\p{N}\s\-]+$");

            if (hasSqlInjectionPattern || !isValidCharacters)
            {
                // Ghi nhận cảnh báo an ninh vào Audit Log
                await _auditLogService.LogAsync(
                    username,
                    role,
                    "/api/orders/search",
                    "GET",
                    400,
                    $"[KB4 Chặn SQLi] Phát hiện chuỗi tìm kiếm không hợp lệ hoặc chứa dấu hiệu SQL Injection: '{trimmedKeyword}'",
                    ip);

                return BadRequest(new
                {
                    success = false,
                    statusCode = 400,
                    message = "[V2 - KB4 Chặn SQLi] Đầu vào không hợp lệ: Từ khóa chứa ký tự đặc biệt không được phép hoặc có dấu hiệu tấn công SQL Injection.",
                    error = "InvalidInputOrSqlInjectionDetected",
                    rejectedKeyword = trimmedKeyword
                });
            }

            // 4. Thực thi an toàn bằng Parameterized EF Core LINQ
            IQueryable<Models.Order> query = _context.Orders.AsNoTracking();

            // Vẫn áp dụng phân quyền phạm vi xem đơn hàng
            if (role != "Admin" && role != "NhanVien")
            {
                query = query.Where(o => o.UserId == userId);
            }

            query = query.Where(o => EF.Functions.Like(o.OrderCode, $"%{trimmedKeyword}%") 
                                  || EF.Functions.Like(o.ProductName, $"%{trimmedKeyword}%"));

            var results = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            await _auditLogService.LogAsync(
                username,
                role,
                "/api/orders/search",
                "GET",
                200,
                $"[KB4 Tìm kiếm thành công] User '{username}' tìm kiếm an toàn với từ khóa '{trimmedKeyword}' (kết quả: {results.Count}).",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V2] Tìm kiếm đơn hàng an toàn thành công (Parameterized Query - Chống SQL Injection tuyệt đối).",
                keyword = trimmedKeyword,
                count = results.Count,
                data = results
            });
        }
    }
}
