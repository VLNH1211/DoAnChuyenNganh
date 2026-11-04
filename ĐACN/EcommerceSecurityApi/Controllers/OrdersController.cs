using System.Security.Claims;
using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.DTOs.Orders;
using EcommerceSecurityApi.Entities;
using EcommerceSecurityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISecurityLogService _securityLogService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(AppDbContext context, ISecurityLogService securityLogService, ILogger<OrdersController> logger)
    {
        _context = context;
        _securityLogService = securityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Đặt mua sản phẩm công nghệ mới (Chống Price Tampering: Giá được lấy trực tiếp từ Database)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Không thể xác định danh tính người dùng từ Bearer Token." });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { success = false, message = "Người dùng không tồn tại." });
        }

        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest(new { success = false, message = "Đơn hàng phải chứa ít nhất một sản phẩm công nghệ." });
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        if (products.Count != productIds.Count)
        {
            return BadRequest(new { success = false, message = "Một số sản phẩm trong giỏ hàng không tồn tại." });
        }

        // Kiểm tra tồn kho và tính toán tổng tiền từ giá niêm yết trong Database (chống giả mạo giá client)
        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);

            if (product.Stock < item.Quantity)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Sản phẩm '{product.Name}' chỉ còn {product.Stock} món trong kho, không đủ đáp ứng số lượng {item.Quantity} món."
                });
            }

            // Trừ tồn kho
            product.Stock -= item.Quantity;

            var itemTotalPrice = product.Price * item.Quantity;
            totalAmount += itemTotalPrice;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                TotalPrice = itemTotalPrice
            });
        }

        var orderCode = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var order = new Order
        {
            OrderCode = orderCode,
            UserId = userId,
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? (user.FullName ?? user.Username) : request.CustomerName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? (user.PhoneNumber ?? "0900000000") : request.PhoneNumber.Trim(),
            ShippingAddress = request.ShippingAddress.Trim(),
            Notes = request.Notes?.Trim(),
            TotalAmount = totalAmount,
            Status = "Pending",
            OrderDate = DateTime.UtcNow,
            OrderItems = orderItems
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Người dùng {User} (ID: {UserId}) đã đặt thành công đơn hàng {OrderCode} trị giá {Total:N0}đ.", user.Username, userId, order.OrderCode, totalAmount);

        var response = await MapToOrderResponseDto(order.Id);

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, new
        {
            success = true,
            message = "Đặt mua sản phẩm công nghệ thành công! Đơn hàng của bạn đang được xử lý.",
            order = response
        });
    }

    /// <summary>
    /// Xem danh sách đơn hàng của chính người dùng hiện tại (BOLA Safe)
    /// </summary>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(List<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Không thể xác định danh tính người dùng." });
        }

        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderResponseDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                UserId = o.UserId,
                Username = o.User != null ? o.User.Username : string.Empty,
                CustomerName = o.CustomerName,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                Notes = o.Notes,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                OrderDate = o.OrderDate,
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.Name : string.Empty,
                    ProductBrand = oi.Product != null ? oi.Product.Brand : string.Empty,
                    ProductImageUrl = oi.Product != null ? oi.Product.ImageUrl : string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Xem chi tiết 1 đơn hàng theo ID (TÍCH HỢP PHÒNG THỦ BOLA / IDOR - OWASP API SECURITY #1)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized(new { success = false, message = "Không trích xuất được danh tính người dùng." });
        }

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy đơn hàng có mã ID {id}." });
        }

        // =========================================================================
        // 🛡️ CƠ CHẾ PHÒNG THỦ BOLA / IDOR (Broken Object Level Authorization):
        // Chỉ cho phép xem nếu:
        // 1. Người dùng có vai trò Quản trị viên (Admin) hoặc Nhân viên (NhanVien).
        // 2. HOẶC người dùng chính là chủ sở hữu đơn hàng (order.UserId == currentUserId).
        // Nếu không thỏa mãn -> Server LẬP TỨC CHẶN BẰNG MÃ 403 FORBIDDEN!
        // =========================================================================
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("NhanVien");
        if (!isStaffOrAdmin && order.UserId != currentUserId)
        {
            _logger.LogWarning("🛡️ [CẢNH BÁO BOLA/IDOR] Người dùng {User} (ID: {UserId}) cố tình truy cập đơn hàng ID {OrderId} của người dùng khác (Owner ID: {OwnerId})!",
                User.Identity?.Name, currentUserId, id, order.UserId);

            await _securityLogService.LogIncidentAsync(
                incidentType: "BOLA_IDOR_VIOLATION",
                severity: "Critical",
                description: $"Người dùng '{User.Identity?.Name}' (ID: {currentUserId}) cố tình đọc trộm đơn hàng #{order.OrderCode} (ID: {order.Id}) của khách hàng ID {order.UserId}.",
                targetResource: $"GET /api/orders/{id}",
                userId: currentUserId,
                username: User.Identity?.Name,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                statusCode = StatusCodes.Status403Forbidden,
                error = "BOLA_SECURITY_VIOLATION",
                message = "🛡️ [PHÒNG THỦ BOLA/IDOR KÍCH HOẠT]: Bạn không có quyền truy cập vào đơn hàng của khách hàng khác!",
                detail = $"Tài khoản của bạn (ID: {currentUserId}) không phải là chủ sở hữu của đơn hàng {order.OrderCode} (Chủ sở hữu ID: {order.UserId}). Yêu cầu bị từ chối tuân theo chuẩn OWASP API Security Top 1.",
                timestamp = DateTime.UtcNow
            });
        }

        var response = new OrderResponseDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            UserId = order.UserId,
            Username = order.User?.Username ?? "N/A",
            CustomerName = order.CustomerName,
            PhoneNumber = order.PhoneNumber,
            ShippingAddress = order.ShippingAddress,
            Notes = order.Notes,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            OrderDate = order.OrderDate,
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.Product != null ? oi.Product.Name : string.Empty,
                ProductBrand = oi.Product != null ? oi.Product.Brand : string.Empty,
                ProductImageUrl = oi.Product != null ? oi.Product.ImageUrl : string.Empty,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Xem toàn bộ đơn hàng trong tiệm (Chỉ Quản trị viên và Nhân viên)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,NhanVien")]
    [ProducesResponseType(typeof(List<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? status)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status.ToLower() == status.Trim().ToLower());
        }

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderResponseDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                UserId = o.UserId,
                Username = o.User != null ? o.User.Username : "N/A",
                CustomerName = o.CustomerName,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                Notes = o.Notes,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                OrderDate = o.OrderDate,
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.Name : string.Empty,
                    ProductBrand = oi.Product != null ? oi.Product.Brand : string.Empty,
                    ProductImageUrl = oi.Product != null ? oi.Product.ImageUrl : string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng (Chỉ Quản trị viên hoặc Nhân viên)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,NhanVien")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy đơn hàng ID {id}." });
        }

        var oldStatus = order.Status;
        order.Status = request.Status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Người dùng {User} đã chuyển trạng thái đơn hàng {OrderCode} từ '{Old}' sang '{New}'.",
            User.Identity?.Name, order.OrderCode, oldStatus, order.Status);

        return Ok(new
        {
            success = true,
            message = $"Cập nhật trạng thái đơn hàng {order.OrderCode} thành '{order.Status}' thành công!",
            orderId = order.Id,
            newStatus = order.Status
        });
    }

    /// <summary>
    /// Thống kê doanh thu và đơn hàng (CHỈ DUY NHẤT ADMIN)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(OrderStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderStats()
    {
        var totalOrders = await _context.Orders.CountAsync();
        var totalRevenue = await _context.Orders
            .Where(o => o.Status == "Completed" || o.Status == "Processing")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var totalProductsSold = await _context.OrderItems
            .Where(oi => oi.Order.Status == "Completed" || oi.Order.Status == "Processing")
            .SumAsync(oi => (int?)oi.Quantity) ?? 0;

        var pending = await _context.Orders.CountAsync(o => o.Status == "Pending");
        var processing = await _context.Orders.CountAsync(o => o.Status == "Processing");
        var completed = await _context.Orders.CountAsync(o => o.Status == "Completed");
        var cancelled = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

        var stats = new OrderStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            TotalProductsSold = totalProductsSold,
            PendingOrders = pending,
            ProcessingOrders = processing,
            CompletedOrders = completed,
            CancelledOrders = cancelled
        };

        return Ok(stats);
    }

    private async Task<OrderResponseDto?> MapToOrderResponseDto(int orderId)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.Id == orderId)
            .Select(o => new OrderResponseDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                UserId = o.UserId,
                Username = o.User != null ? o.User.Username : string.Empty,
                CustomerName = o.CustomerName,
                PhoneNumber = o.PhoneNumber,
                ShippingAddress = o.ShippingAddress,
                Notes = o.Notes,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                OrderDate = o.OrderDate,
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.Name : string.Empty,
                    ProductBrand = oi.Product != null ? oi.Product.Brand : string.Empty,
                    ProductImageUrl = oi.Product != null ? oi.Product.ImageUrl : string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
