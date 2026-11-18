using System.Security.Claims;
using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.DTOs.Admin;
using EcommerceSecurityApi.Helpers;
using EcommerceSecurityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISecurityLogService _securityLogService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext context, ISecurityLogService securityLogService, ILogger<AdminController> logger)
    {
        _context = context;
        _securityLogService = securityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ người dùng trong hệ thống kèm trạng thái khóa (Chỉ Admin)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
    {
        var rawUsers = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Id)
            .ToListAsync();

        var users = rawUsers.Select(u => new UserAdminDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            FullName = u.FullName,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            FailedLoginAttempts = u.FailedLoginAttempts,
            CreatedAt = DateTime.SpecifyKind(u.CreatedAt, DateTimeKind.Utc),
            LastLoginAt = u.LastLoginAt.HasValue ? DateTime.SpecifyKind(u.LastLoginAt.Value, DateTimeKind.Utc) : null,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        }).ToList();

        return Ok(users);
    }

    /// <summary>
    /// Bật/Tắt trạng thái hoạt động (Khóa / Mở khóa) của tài khoản người dùng
    /// </summary>
    [HttpPut("users/{id}/toggle-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdClaim, out int currentUserId);

        if (id == currentUserId)
        {
            return BadRequest(new { success = false, message = "Bạn không thể tự khóa tài khoản Quản trị viên đang đăng nhập của chính mình!" });
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy người dùng có ID {id}." });
        }

        // Đảo trạng thái IsActive
        user.IsActive = !user.IsActive;
        if (user.IsActive)
        {
            user.FailedLoginAttempts = 0; // Reset số lần sai khi mở khóa
        }

        await _context.SaveChangesAsync();

        string actionText = user.IsActive ? "Mở khóa thành công" : "Khóa tài khoản thành công";
        string logMsg = $"Quản trị viên '{User.Identity?.Name}' đã {(user.IsActive ? "MỞ KHÓA" : "KHÓA")} tài khoản '{user.Username}' (ID: {user.Id}).";

        await _securityLogService.LogIncidentAsync(
            incidentType: user.IsActive ? "ACCOUNT_UNLOCKED" : "ACCOUNT_LOCKED_BY_ADMIN",
            severity: user.IsActive ? "Info" : "Critical",
            description: logMsg,
            targetResource: $"PUT /api/admin/users/{id}/toggle-status",
            userId: user.Id,
            username: user.Username,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(new
        {
            success = true,
            userId = user.Id,
            username = user.Username,
            isActive = user.IsActive,
            message = $"{actionText}: Tài khoản '{user.Username}' hiện {(user.IsActive ? "ĐANG HOẠT ĐỘNG" : "ĐÃ BỊ ĐÌNH CHỈ / KHÓA")}!"
        });
    }

    /// <summary>
    /// Xóa vĩnh viễn tài khoản người dùng (Chỉ Admin, bảo vệ không tự xóa chính mình hoặc Admin gốc)
    /// </summary>
    [HttpDelete("users/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdClaim, out int currentUserId);

        if (id == currentUserId)
        {
            return BadRequest(new { success = false, message = "Bạn không thể tự xóa tài khoản Quản trị viên đang đăng nhập của chính mình!" });
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy người dùng có ID {id}." });
        }

        if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "Không thể xóa tài khoản Quản trị viên hệ thống mặc định!" });
        }

        var deletedUsername = user.Username;
        var deletedEmail = user.Email;

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
        {
            transaction = await _context.Database.BeginTransactionAsync();
        }

        try
        {
            // Xử lý sạch các đơn hàng liên quan (tránh lỗi ràng buộc khóa ngoại Orders -> Users)
            var userOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == id)
                .ToListAsync();

            if (userOrders.Count > 0)
            {
                var orderItems = userOrders.SelectMany(o => o.OrderItems).ToList();
                var pendingItems = userOrders
                    .Where(o => o.Status == "Pending" || o.Status == "Processing")
                    .SelectMany(o => o.OrderItems)
                    .ToList();

                if (pendingItems.Count > 0)
                {
                    var productIds = pendingItems.Select(i => i.ProductId).Distinct().ToList();
                    var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
                    foreach (var item in pendingItems)
                    {
                        var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            product.Stock += item.Quantity;
                        }
                    }
                }

                if (orderItems.Count > 0)
                {
                    _context.OrderItems.RemoveRange(orderItems);
                }
                _context.Orders.RemoveRange(userOrders);
            }

            if (user.UserRoles.Count > 0)
            {
                _context.UserRoles.RemoveRange(user.UserRoles);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }

        await _securityLogService.LogIncidentAsync(
            incidentType: "ACCOUNT_DELETED_BY_ADMIN",
            severity: "Warning",
            description: $"Quản trị viên '{User.Identity?.Name}' đã XÓA VĨNH VIỄN tài khoản '{deletedUsername}' (ID: {id}, Email: {deletedEmail}).",
            targetResource: $"DELETE /api/admin/users/{id}",
            userId: id,
            username: deletedUsername,
            ipAddress: ClientInfoHelper.GetClientIpAddress(HttpContext)
        );

        _logger.LogWarning("Quản trị viên {Admin} đã xóa tài khoản {Username} (ID: {Id}).", User.Identity?.Name, deletedUsername, id);

        return Ok(new
        {
            success = true,
            userId = id,
            username = deletedUsername,
            message = $"Đã xóa vĩnh viễn tài khoản '{deletedUsername}' (#{id}) khỏi hệ thống!"
        });
    }

    /// <summary>
    /// Lấy danh sách các sự cố vi phạm an toàn thông tin gần nhất (Chỉ Admin)
    /// </summary>
    [HttpGet("security-logs")]
    [ProducesResponseType(typeof(List<SecurityLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSecurityLogs([FromQuery] int count = 50)
    {
        var logs = await _securityLogService.GetRecentLogsAsync(count);
        return Ok(logs);
    }

    /// <summary>
    /// Xóa toàn bộ nhật ký an ninh (Làm mới bảng log phục vụ demo)
    /// </summary>
    [HttpDelete("security-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearSecurityLogs()
    {
        _context.SecurityLogs.RemoveRange(_context.SecurityLogs);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã làm sạch toàn bộ nhật ký an ninh hệ thống." });
    }
}
