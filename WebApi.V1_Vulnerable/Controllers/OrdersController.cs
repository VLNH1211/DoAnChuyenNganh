using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.V1_Vulnerable.Data;
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

            // V1: Truy vấn thẳng theo Id, không biết hoặc bỏ qua định danh người gọi
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
    }
}
