using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.V2_Secure.Data;
using WebApi.V2_Secure.DTOs;
using WebApi.V2_Secure.Models;
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
        private readonly IRateLimitingService _rateLimitingService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            V2DbContext context,
            IAuditLogService auditLogService,
            IRateLimitingService rateLimitingService,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _rateLimitingService = rateLimitingService;
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

            IQueryable<Order> query = _context.Orders.AsNoTracking();

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
            if (order.UserId != userId && role != "Admin")
            {
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
            string[] dangerousPatterns = { "'", "\"", ";", "--", "/*", "*/", "xp_", "exec", "union", "or 1=1", "drop ", "alter ", "delete ", "insert " };
            bool hasSqlInjectionPattern = dangerousPatterns.Any(p => trimmedKeyword.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
            bool isValidCharacters = Regex.IsMatch(trimmedKeyword, @"^[\p{L}\p{N}\s\-]+$");

            if (hasSqlInjectionPattern || !isValidCharacters)
            {
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

            // 4. Parameterized EF Core LINQ
            IQueryable<Order> query = _context.Orders.AsNoTracking();

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

        /// <summary>
        /// V2: Tạo đơn hàng mới - Chống Mass Assignment & Kiểm soát Logic Nghiệp vụ
        /// Tự động trích xuất UserId từ JWT Claims, bắt buộc số tiền > 0 VNĐ, trạng thái mặc định 'Pending'.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var (userId, username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    statusCode = 400,
                    message = "Dữ liệu đầu vào không hợp lệ.",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // Kiểm tra Business Logic: Số tiền phải > 0
            if (request.TotalAmount <= 0)
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    "/api/orders",
                    "POST",
                    400,
                    $"[Chặn Business Logic Flaw] Người dùng '{username}' cố gắng tạo đơn hàng với số tiền không hợp lệ: {request.TotalAmount} VNĐ.",
                    ip);

                return BadRequest(new
                {
                    success = false,
                    statusCode = 400,
                    message = "[V2 - Chặn Lỗi Nghiệp Vụ] Số tiền đơn hàng phải lớn hơn 0 VNĐ.",
                    error = "InvalidTotalAmount"
                });
            }

            // Lấy thông tin họ tên user từ DB
            var dbUser = await _context.Users.FindAsync(userId);
            var customerName = dbUser?.FullName ?? username;

            var newOrder = new Order
            {
                OrderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                UserId = userId, // CHỐNG MASS ASSIGNMENT: Tự động gán UserId từ JWT Token
                CustomerName = customerName,
                ProductName = request.ProductName.Trim(),
                TotalAmount = request.TotalAmount,
                Status = "Pending", // Khách hàng không thể tự đặt Completed
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                username,
                role,
                "/api/orders",
                "POST",
                201,
                $"[Tạo Đơn Hàng Thành Công] User '{username}' đã tạo đơn #{newOrder.Id} ({newOrder.OrderCode}) cho sản phẩm '{newOrder.ProductName}'.",
                ip);

            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                statusCode = 201,
                message = "[V2] Tạo đơn hàng mới thành công (Đã phòng chống Mass Assignment & Kiểm tra Logic nghiệp vụ).",
                data = newOrder
            });
        }

        /// <summary>
        /// V2: Cập nhật đơn hàng - Phòng chống IDOR khi Sửa & Kiểm soát Phân quyền Trạng thái
        /// Chỉ chủ sở hữu (hoặc Admin) mới được sửa đơn; Chỉ Admin/Staff mới được duyệt 'Completed'.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
        {
            var (userId, username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { success = false, statusCode = 404, message = $"Không tìm thấy đơn hàng ID={id}" });
            }

            // 1. KIỂM TRA CHỦ SỞ HỮU (Chống IDOR khi Sửa)
            if (order.UserId != userId && role != "Admin")
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    $"/api/orders/{id}",
                    "PUT",
                    403,
                    $"[Chặn IDOR khi Sửa] Người dùng '{username}' (UserId: {userId}) cố gắng sửa trái phép đơn hàng #{id} của UserId {order.UserId}.",
                    ip);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    statusCode = 403,
                    message = "[V2 - Chặn IDOR khi Sửa] Truy cập bị từ chối: Bạn không phải chủ sở hữu của đơn hàng này.",
                    error = "Forbidden_CannotEditOthersOrder"
                });
            }

            // 2. KIỂM TRA LOGIC NGHIỆP VỤ & PHÂN QUYỀN TRẠNG THÁI
            if (request.TotalAmount <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    statusCode = 400,
                    message = "[V2] Số tiền đơn hàng phải lớn hơn 0 VNĐ.",
                    error = "InvalidTotalAmount"
                });
            }

            if (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                if (role != "Admin" && role != "NhanVien")
                {
                    await _auditLogService.LogAsync(
                        username,
                        role,
                        $"/api/orders/{id}",
                        "PUT",
                        403,
                        $"[Chặn Vượt Quyền Trạng Thái] Khách hàng '{username}' cố gắng tự thăng hạng đơn #{id} thành 'Completed'.",
                        ip);

                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        success = false,
                        statusCode = 403,
                        message = "[V2 - Phân Quyền Trạng Thái] Chỉ Quản trị viên (Admin) hoặc Nhân viên mới có quyền duyệt hoàn tất (Completed) đơn hàng.",
                        error = "Forbidden_StatusTransitionRestricted"
                    });
                }
                order.Status = "Completed";
            }
            else if (!string.IsNullOrWhiteSpace(request.Status))
            {
                order.Status = request.Status;
            }

            order.ProductName = request.ProductName.Trim();
            order.TotalAmount = request.TotalAmount;

            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                username,
                role,
                $"/api/orders/{id}",
                "PUT",
                200,
                $"[Cập nhật Đơn Hàng Thành Công] User '{username}' cập nhật đơn hàng #{id}.",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = $"[V2] Cập nhật đơn hàng #{id} thành công (Đã xác minh quyền sở hữu và logic nghiệp vụ).",
                data = order
            });
        }

        /// <summary>
        /// V2: Hủy/Xóa đơn hàng - Phòng chống IDOR khi Xóa
        /// Chỉ chủ sở hữu (hoặc Admin) mới được phép hủy đơn hàng.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var (userId, username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { success = false, statusCode = 404, message = $"Không tìm thấy đơn hàng ID={id}" });
            }

            // KIỂM TRA CHỦ SỞ HỮU (Chống IDOR khi Xóa)
            if (order.UserId != userId && role != "Admin")
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    $"/api/orders/{id}",
                    "DELETE",
                    403,
                    $"[Chặn IDOR khi Xóa] Người dùng '{username}' (UserId: {userId}) cố gắng xóa trái phép đơn hàng #{id} của UserId {order.UserId}.",
                    ip);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    statusCode = 403,
                    message = "[V2 - Chặn IDOR khi Xóa] Truy cập bị từ chối: Bạn không có quyền hủy/xóa đơn hàng của người khác.",
                    error = "Forbidden_CannotDeleteOthersOrder"
                });
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                username,
                role,
                $"/api/orders/{id}",
                "DELETE",
                200,
                $"[Xóa Đơn Hàng Thành Công] User '{username}' đã xóa/hủy đơn hàng #{id}.",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = $"[V2] Đã xóa/hủy đơn hàng #{id} thành công (Đã xác thực quyền sở hữu hợp lệ)."
            });
        }

        /// <summary>
        /// V2: Tải lên Chứng từ / Hóa đơn - Kiểm tra Quyền sở hữu, Chữ ký Byte (Magic Bytes) & Chống Path Traversal
        /// Chỉ cho phép .jpg, .png, .pdf; Bắt buộc kiểm tra chữ ký byte thực tế đầu tệp.
        /// </summary>
        [HttpPost("{id}/upload")]
        public async Task<IActionResult> UploadAttachment(int id, IFormFile? file)
        {
            var (userId, username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { success = false, statusCode = 404, message = $"Không tìm thấy đơn hàng ID={id}" });
            }

            // 1. Kiểm tra quyền sở hữu đơn hàng
            if (order.UserId != userId && role != "Admin")
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    $"/api/orders/{id}/upload",
                    "POST",
                    403,
                    $"[Chặn Upload Trái Phép] User '{username}' cố tình upload file vào đơn #{id} của người khác.",
                    ip);

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    statusCode = 403,
                    message = "Bạn không có quyền tải tài liệu lên đơn hàng này.",
                    error = "Forbidden_CannotUploadToOthersOrder"
                });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, statusCode = 400, message = "Vui lòng chọn tệp tin cần tải lên." });
            }

            // Giới hạn dung lượng tối đa 5MB
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { success = false, statusCode = 400, message = "Dung lượng tệp tin vượt quá giới hạn 5MB." });
            }

            var originalFileName = file.FileName;

            // 2. Chống Path Traversal & Tên tệp nguy hiểm
            if (originalFileName.Contains("..") || originalFileName.Contains("/") || originalFileName.Contains("\\"))
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    $"/api/orders/{id}/upload",
                    "POST",
                    400,
                    $"[Chặn Path Traversal] Tên tệp '{originalFileName}' chứa ký tự điều hướng thư mục nguy hiểm.",
                    ip);

                return BadRequest(new
                {
                    success = false,
                    statusCode = 400,
                    message = "[V2 - Chặn Path Traversal] Tên tệp tin không hợp lệ: Chứa ký tự điều hướng thư mục nguy hiểm.",
                    error = "PathTraversalDetected"
                });
            }

            // 3. Chống Double Extension (Ví dụ: webshell.jpg.aspx hoặc evil.png.exe)
            var lowerFileName = originalFileName.ToLowerInvariant();
            string[] dangerousExts = { ".aspx", ".exe", ".sh", ".php", ".bat", ".cmd", ".ps1", ".vbs", ".jsp" };
            if (dangerousExts.Any(ext => lowerFileName.Contains(ext)))
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    $"/api/orders/{id}/upload",
                    "POST",
                    400,
                    $"[Chặn Tệp Độc Hại] Phát hiện đuôi mở rộng thực thi nguy hiểm trong tên tệp: '{originalFileName}'.",
                    ip);

                return BadRequest(new
                {
                    success = false,
                    statusCode = 400,
                    message = "[V2 - Chặn Tệp Độc Hại] Phát hiện đuôi mở rộng thực thi nguy hiểm hoặc kỹ thuật Double Extension.",
                    error = "DangerousFileExtensionDetected"
                });
            }

            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            string[] allowedExts = { ".jpg", ".jpeg", ".png", ".pdf" };
            if (!allowedExts.Contains(ext))
            {
                return StatusCode(StatusCodes.Status415UnsupportedMediaType, new
                {
                    success = false,
                    statusCode = 415,
                    message = "[V2] Định dạng tệp không được hỗ trợ. Chỉ chấp nhận các định dạng ảnh/chứng từ: .jpg, .png, .pdf.",
                    error = "UnsupportedMediaType"
                });
            }

            // 4. KIỂM TRA CHỮ KÝ BYTE THỰC TẾ (MAGIC BYTES VALIDATION)
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[8];
            var bytesRead = await stream.ReadAsync(headerBytes, 0, headerBytes.Length);

            bool isSignatureValid = false;
            string detectedType = "Unknown";

            if (bytesRead >= 3 && headerBytes[0] == 0xFF && headerBytes[1] == 0xD8 && headerBytes[2] == 0xFF)
            {
                // JPG/JPEG magic bytes: FF D8 FF
                isSignatureValid = (ext == ".jpg" || ext == ".jpeg");
                detectedType = "image/jpeg";
            }
            else if (bytesRead >= 4 && headerBytes[0] == 0x89 && headerBytes[1] == 0x50 && headerBytes[2] == 0x4E && headerBytes[3] == 0x47)
            {
                // PNG magic bytes: 89 50 4E 47
                isSignatureValid = (ext == ".png");
                detectedType = "image/png";
            }
            else if (bytesRead >= 4 && headerBytes[0] == 0x25 && headerBytes[1] == 0x50 && headerBytes[2] == 0x44 && headerBytes[3] == 0x46)
            {
                // PDF magic bytes: 25 50 44 46 (%PDF)
                isSignatureValid = (ext == ".pdf");
                detectedType = "application/pdf";
            }

            if (!isSignatureValid)
            {
                await _auditLogService.LogAsync(
                    username,
                    role,
                    $"/api/orders/{id}/upload",
                    "POST",
                    415,
                    $"[Chặn Giả Mạo Magic Bytes] Tệp '{originalFileName}' khai báo đuôi '{ext}' nhưng chữ ký byte thực tế không khớp hoặc chứa mã độc.",
                    ip);

                return StatusCode(StatusCodes.Status415UnsupportedMediaType, new
                {
                    success = false,
                    statusCode = 415,
                    message = "[V2 - Chặn Giả Mạo Magic Bytes] Phát hiện nội dung tệp tin không khớp với phần mở rộng hoặc giả mạo định dạng. Tệp bị từ chối để ngăn chặn mã độc Webshell / RCE.",
                    error = "MagicBytesMismatch",
                    declaredExtension = ext,
                    detectedSignature = BitConverter.ToString(headerBytes, 0, Math.Min(bytesRead, 4))
                });
            }

            // Đổi tên tệp lưu trữ thành GUID ngẫu nhiên an toàn
            var safeStoredFileName = $"{Guid.NewGuid()}{ext}";

            await _auditLogService.LogAsync(
                username,
                role,
                $"/api/orders/{id}/upload",
                "POST",
                200,
                $"[Upload Thành Công] Tải lên chứng từ an toàn '{originalFileName}' (Lưu trữ: {safeStoredFileName}) cho đơn #{id}.",
                ip);

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V2 - Upload An Toàn] Tải lên chứng từ thành công (Đã kiểm tra Magic Bytes, chống Double Extension & Path Traversal).",
                originalFileName,
                safeStoredFileName,
                fileSize = file.Length,
                mimeType = detectedType
            });
        }

        /// <summary>
        /// V2: Endpoint kiểm thử Rate Limiting & Chống Spam API (Tối đa 5 req / 5s)
        /// Vượt ngưỡng sẽ trả về 429 Too Many Requests kèm Retry-After Header.
        /// </summary>
        [HttpGet("rate-test")]
        public async Task<IActionResult> RateLimitTest()
        {
            var (userId, username, role) = GetCurrentUserInfo();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var rateKey = $"ratetest:{ip}:{userId}";

            // Giới hạn 5 yêu cầu trong 5 giây
            if (!_rateLimitingService.IsAllowed(rateKey, 5, TimeSpan.FromSeconds(5), out int retryAfter))
            {
                Response.Headers["Retry-After"] = retryAfter.ToString();

                await _auditLogService.LogAsync(
                    username,
                    role,
                    "/api/orders/rate-test",
                    "GET",
                    429,
                    $"[Chặn Rate Limit] User '{username}' (IP: {ip}) gửi yêu cầu vượt ngưỡng 5 requests / 5 giây. Chặn tạm thời {retryAfter}s.",
                    ip);

                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    success = false,
                    statusCode = 429,
                    message = $"[V2 - Chặn Rate Limit] Quá nhiều yêu cầu được gửi trong thời gian ngắn (Vượt ngưỡng 5 req / 5s). Vui lòng thử lại sau {retryAfter} giây.",
                    error = "RateLimitExceeded",
                    retryAfterSeconds = retryAfter
                });
            }

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V2 - Trong Ngưỡng Cho Phép] Yêu cầu hợp lệ, tần suất gọi API nằm trong hạn mức an toàn.",
                userContext = new { username, role }
            });
        }
    }
}
