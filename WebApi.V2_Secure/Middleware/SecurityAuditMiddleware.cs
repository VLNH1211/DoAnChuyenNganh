using System.Security.Claims;
using System.Text.Json;
using WebApi.V2_Secure.Services;

namespace WebApi.V2_Secure.Middleware
{
    public class SecurityAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityAuditMiddleware> _logger;

        public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            var endpoint = context.Request.Path.Value ?? "";
            var method = context.Request.Method;

            // Bỏ qua Swagger assets để tránh làm loãng Audit Log
            if (endpoint.StartsWith("/swagger") || endpoint.StartsWith("/favicon.ico"))
            {
                await _next(context);
                return;
            }

            // Gọi các middleware tiếp theo trong pipeline
            await _next(context);

            var statusCode = context.Response.StatusCode;
            var username = context.User.Identity?.IsAuthenticated == true
                ? (context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.Identity.Name)
                : "Anonymous";

            var role = context.User.Identity?.IsAuthenticated == true
                ? (context.User.FindFirst(ClaimTypes.Role)?.Value ?? "None")
                : "None";

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Xử lý các mã lỗi chặn bảo mật nếu pipeline chưa ghi đè body
            if (statusCode == 401 && !context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var errorResponse = new
                {
                    success = false,
                    statusCode = 401,
                    message = "[V2 - KB1] Xác thực thất bại: Endpoint yêu cầu JWT Bearer Token hợp lệ.",
                    error = "Unauthorized",
                    endpoint = endpoint,
                    timestamp = DateTime.UtcNow
                };

                await auditLogService.LogAsync(
                    username,
                    role,
                    endpoint,
                    method,
                    401,
                    "[KB1 Xác thực thất bại] Yêu cầu không có JWT Token hợp lệ hoặc token đã hết hạn.",
                    ipAddress);

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                return;
            }
            else if (statusCode == 403 && !context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var errorResponse = new
                {
                    success = false,
                    statusCode = 403,
                    message = "[V2 - KB2] Quyền truy cập bị từ chối: Tài khoản không có vai trò hợp lệ (Yêu cầu quyền Admin).",
                    error = "Forbidden",
                    endpoint = endpoint,
                    timestamp = DateTime.UtcNow
                };

                await auditLogService.LogAsync(
                    username,
                    role,
                    endpoint,
                    method,
                    403,
                    $"[KB2 Phân quyền thất bại] User '{username}' vai trò '{role}' cố gắng truy cập endpoint yêu cầu Admin.",
                    ipAddress);

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                return;
            }
        }
    }
}
