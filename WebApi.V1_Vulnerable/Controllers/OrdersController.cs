using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.V1_Vulnerable.Data;
using WebApi.V1_Vulnerable.DTOs;
using WebApi.V1_Vulnerable.Models;

namespace WebApi.V1_Vulnerable.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly V1DbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(V1DbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// KB1 - Bản v1 (Before): KHÔNG có [Authorize]
        /// Gọi không cần token vẫn trả về 200 OK kèm toàn bộ danh sách đơn hàng.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            _logger.LogInformation("[V1 - KB1] GetAllOrders: Truy cập không cần xác thực (Thiếu Authorize)");
            
            var orders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "[V1] Lấy danh sách đơn hàng thành công (Không yêu cầu đăng nhập/Token - KB1 Lỗ hổng xác thực).",
                total = orders.Count,
                data = orders
            });
        }

        /// <summary>
        /// KB3 - Bản v1 (Before): Truy cập dữ liệu tài khoản khác (BOLA/IDOR)
        /// Chỉ truy vấn theo {id}, KHÔNG kiểm tra UserId chủ sở hữu đơn hàng.
        /// Ví dụ: userA đổi id = 3 (đơn của userB) vẫn nhận được 200 OK.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            _logger.LogInformation("[V1 - KB3] GetOrderById: Truy vấn đơn hàng ID={Id} không kiểm tra chủ sở hữu", id);

            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"[V1] Không tìm thấy đơn hàng có mã ID = {id}."
                });
            }

            return Ok(new
            {
                success = true,
                message = $"[V1] Lấy thông tin đơn hàng #{id} thành công (Lỗ hổng BOLA/IDOR: Không kiểm tra quyền sở hữu).",
                data = order
            });
        }

        /// <summary>
        /// KB4 - Bản v1 (Before): Tìm kiếm đơn hàng - Lỗ hổng SQL Injection có kiểm soát
        /// Ghép chuỗi trực tiếp vào câu lệnh SQL, không kiểm tra hay lọc đầu vào.
        /// Thử nghiệm: keyword = ' OR 1=1 --
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchOrders([FromQuery] string? keyword)
        {
            _logger.LogInformation("[V1 - KB4] SearchOrders: Ghép chuỗi thô vào SQL với keyword='{Keyword}'", keyword);

            if (keyword == null) keyword = "";

            try
            {
                // V1: Lỗ hổng SQL Injection do ghép chuỗi thô trực tiếp
                string sqlQuery = $"SELECT * FROM Orders WHERE OrderCode LIKE '%{keyword}%' OR ProductName LIKE '%{keyword}%'";

                var results = await _context.Orders
                    .FromSqlRaw(sqlQuery)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = $"[V1] Tìm kiếm đơn hàng thành công với câu lệnh thô (Lỗ hổng SQL Injection).",
                    queryExecuted = sqlQuery,
                    count = results.Count,
                    data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V1 - KB4] Lỗi thực thi câu lệnh SQL do chuỗi injection không hợp lệ cú pháp");
                
                // Trả về lỗi 500 kèm chi tiết ngoại lệ của SQL Server (Lộ thông tin hệ thống)
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi máy chủ nội bộ (500 Internal Server Error) khi thực thi SQL:",
                    errorDetail = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// V1: Tạo đơn hàng mới - Lỗ hổng Mass Assignment & Business Logic Flaw
        /// Cho phép truyền số tiền âm, tự chỉ định UserId của người khác hoặc tự đặt trạng thái 'Completed'.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestV1 request)
        {
            var assignedUserId = request.UserId ?? 1;
            var customerName = request.CustomerName ?? "Khách Hàng V1";

            // V1: Không validate số tiền (cho phép số âm), không validate quyền sở hữu (Mass Assignment)
            var newOrder = new Order
            {
                OrderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                UserId = assignedUserId,
                CustomerName = customerName,
                ProductName = request.ProductName ?? "Sản phẩm thử nghiệm",
                TotalAmount = request.TotalAmount, // Cho phép số tiền âm hoặc 0!
                Status = !string.IsNullOrWhiteSpace(request.Status) ? request.Status : "Pending", // Cho phép tự thăng hạng Completed!
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V1 - Lỗ hổng Mass Assignment & Business Logic] Tạo đơn hàng thành công (Hệ thống chấp nhận số tiền âm, tự đặt UserId và Status mà không kiểm tra).",
                data = newOrder
            });
        }

        /// <summary>
        /// V1: Cập nhật đơn hàng - Lỗ hổng IDOR khi sửa
        /// Không kiểm tra quyền sở hữu, cho phép sửa bất kỳ đơn hàng nào của người khác.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequestV1 request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { success = false, message = $"Không tìm thấy đơn hàng ID={id}" });
            }

            // V1: Không kiểm tra UserId chủ đơn hàng -> userA sửa được đơn của userB
            if (!string.IsNullOrWhiteSpace(request.ProductName)) order.ProductName = request.ProductName;
            order.TotalAmount = request.TotalAmount; // Cho phép sửa thành số âm
            if (!string.IsNullOrWhiteSpace(request.Status)) order.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = $"[V1 - Lỗ hổng IDOR khi Sửa] Cập nhật đơn hàng #{id} thành công (Không kiểm tra quyền sở hữu người gọi).",
                data = order
            });
        }

        /// <summary>
        /// V1: Xóa đơn hàng - Lỗ hổng IDOR khi xóa
        /// Không kiểm tra quyền sở hữu, userA xóa được đơn hàng của userB.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { success = false, message = $"Không tìm thấy đơn hàng ID={id}" });
            }

            // V1: Xóa thẳng mà không kiểm tra quyền sở hữu
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = $"[V1 - Lỗ hổng IDOR khi Xóa] Đã xóa thành công đơn hàng #{id} (Bất kỳ ai cũng có thể xóa đơn của người khác)."
            });
        }

        /// <summary>
        /// V1: Upload tệp đính kèm / chứng từ - Lỗ hổng File Upload & Path Traversal
        /// Không kiểm tra Magic Bytes, chấp nhận mọi loại tệp độc hại và không lọc đường dẫn.
        /// </summary>
        [HttpPost("{id}/upload")]
        public IActionResult UploadAttachment(int id, [FromForm] IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn file tải lên." });
            }

            // V1: Nhận mọi file, kể cả webshell.aspx, shell.php, ../../evil.sh
            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = $"[V1 - Lỗ hổng File Upload] Tải lên tệp '{file.FileName}' cho đơn hàng #{id} thành công (Không kiểm tra Magic Bytes / Extension / Path Traversal).",
                fileName = file.FileName,
                fileSize = file.Length,
                contentType = file.ContentType
            });
        }

        /// <summary>
        /// V1: Endpoint kiểm thử Rate Limiting - Không giới hạn tần suất
        /// </summary>
        [HttpGet("rate-test")]
        public IActionResult RateLimitTest()
        {
            return Ok(new
            {
                success = true,
                statusCode = 200,
                message = "[V1 - Không Giới Hạn Tần Suất] Yêu cầu được chấp nhận ngay lập tức (Không áp dụng Rate Limiting / Chống Brute-Force)."
            });
        }
    }
}
