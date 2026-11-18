using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.Services;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Middlewares;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, ITokenBlacklistService blacklistService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // 1. Kiểm tra Token Blacklist (OWASP API2: Chống sử dụng lại Token đã Đăng xuất / Revoke)
            var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                      ?? context.User.FindFirst("jti")?.Value;

            if (!string.IsNullOrEmpty(jti) && blacklistService.IsTokenRevoked(jti))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var revokedObj = new
                {
                    success = false,
                    statusCode = StatusCodes.Status401Unauthorized,
                    error = "TOKEN_REVOKED",
                    message = "🛡️ [PHIÊN ĐĂNG NHẬP ĐÃ HẾT HIỆU LỰC]: Mã JWT Token này đã bị thu hồi do người dùng đã đăng xuất khỏi hệ thống. Vui lòng đăng nhập lại!",
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(revokedObj));
                return;
            }

            // 2. Kiểm tra cờ IsActive của tài khoản trong Database
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var isActive = await dbContext.Users
                    .Where(u => u.Id == userId)
                    .Select(u => (bool?)u.IsActive)
                    .FirstOrDefaultAsync();

                if (!isActive.HasValue)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var deletedObj = new
                    {
                        success = false,
                        statusCode = StatusCodes.Status401Unauthorized,
                        error = "ACCOUNT_DELETED",
                        message = "🛡️ [TÀI KHOẢN KHÔNG TỒN TẠI]: Tài khoản này đã bị Quản trị viên xóa khỏi hệ thống!",
                        timestamp = DateTime.UtcNow
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(deletedObj));
                    return;
                }

                if (!isActive.Value)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var responseObj = new
                    {
                        success = false,
                        statusCode = StatusCodes.Status403Forbidden,
                        error = "ACCOUNT_SUSPENDED",
                        message = "🛡️ [TÀI KHOẢN BỊ KHÓA]: Tài khoản của bạn đã bị Quản trị viên vô hiệu hóa do phát hiện hành vi vi phạm an toàn thông tin!",
                        timestamp = DateTime.UtcNow
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
                    return;
                }
            }
        }

        await _next(context);
    }
}
