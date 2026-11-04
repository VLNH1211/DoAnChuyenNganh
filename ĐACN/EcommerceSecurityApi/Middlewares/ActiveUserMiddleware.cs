using System.Security.Claims;
using System.Text.Json;
using EcommerceSecurityApi.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Middlewares;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                // Truy vấn nhanh cờ IsActive của tài khoản
                var isActive = await dbContext.Users
                    .Where(u => u.Id == userId)
                    .Select(u => (bool?)u.IsActive)
                    .FirstOrDefaultAsync();

                if (isActive.HasValue && !isActive.Value)
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
