using System.IdentityModel.Tokens.Jwt;
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

        public async Task InvokeAsync(
            HttpContext context, 
            IAuditLogService auditLogService,
            ITokenBlacklistService tokenBlacklistService)
        {
            // 1. Gắn các OWASP HTTP Security Headers chuẩn bảo mật cao
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["X-XSS-Protection"] = "1; mode=block";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Content-Security-Policy"] = "default-src 'self' 'unsafe-inline' data:;";
                return Task.CompletedTask;
            });

            var endpoint = context.Request.Path.Value ?? "";
            var method = context.Request.Method;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // 2. Kiểm tra Token Revocation (JTI Blacklist) cho các yêu cầu có JWT Bearer
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                       ?? context.User.FindFirst("jti")?.Value;

                var username = context.User.FindFirst(ClaimTypes.Name)?.Value
                            ?? context.User.FindFirst("unique_name")?.Value
                            ?? "AuthenticatedUser";

                var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                        ?? context.User.FindFirst("role")?.Value
                        ?? "NguoiDung";

                if (!string.IsNullOrEmpty(jti) && tokenBlacklistService.IsTokenRevoked(jti))
                {
                    _logger.LogWarning("[SecurityAuditMiddleware] Token JTI '{Jti}' đã bị thu hồi/đăng xuất", jti);

                    await auditLogService.LogAsync(
                        username,
                        role,
                        endpoint,
                        method,
                        401,
                        $"[KB1 Mở rộng - Thu hồi Token] Phát hiện Token Replay: Token JTI '{jti}' đã bị đăng xuất/thu hồi.",
                        ipAddress);

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    var revokedResponse = new
                    {
                        success = false,
                        statusCode = 401,
                        message = "[V2 - Thu hồi Token] Phiên làm việc (JWT Token) này đã bị thu hồi do người dùng đã đăng xuất (Token Revoked / JTI Blacklist). Vui lòng đăng nhập lại.",
                        error = "TokenRevoked",
                        jti = jti,
                        endpoint = endpoint,
                        timestamp = DateTime.UtcNow
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(revokedResponse));
                    return;
                }
            }

            // Chỉ audit các yêu cầu API (/api/...) để tránh làm loãng Audit Log bởi static web assets và Swagger
            if (!endpoint.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Tiếp tục pipeline
            await _next(context);

            var statusCode = context.Response.StatusCode;
            var currentUser = context.User.Identity?.IsAuthenticated == true
                ? (context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.Identity.Name)
                : "Anonymous";

            var currentRole = context.User.Identity?.IsAuthenticated == true
                ? (context.User.FindFirst(ClaimTypes.Role)?.Value ?? "None")
                : "None";

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
                    currentUser ?? "Anonymous",
                    currentRole,
                    endpoint,
                    method,
                    401,
                    "[KB1 Xác thực thất bại] Yêu cầu không có JWT Token hợp lệ hoặc token đã hết hạn.",
                    ipAddress);

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
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
                    currentUser ?? "Anonymous",
                    currentRole,
                    endpoint,
                    method,
                    403,
                    $"[KB2 Phân quyền thất bại] User '{currentUser}' vai trò '{currentRole}' cố gắng truy cập endpoint yêu cầu Admin.",
                    ipAddress);

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            }
            else if (statusCode == 429 && !context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var errorResponse = new
                {
                    success = false,
                    statusCode = 429,
                    message = "[V2 - Rate Limit] Quá nhiều yêu cầu được gửi trong thời gian ngắn (429 Too Many Requests).",
                    error = "RateLimitExceeded",
                    endpoint = endpoint,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            }
        }
    }
}
