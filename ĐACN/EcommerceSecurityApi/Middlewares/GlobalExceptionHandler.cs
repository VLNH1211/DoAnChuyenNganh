using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EcommerceSecurityApi.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        // Ghi lại đầy đủ Stack Trace và nội dung lỗi trên Server để quản trị viên điều tra sự cố
        _logger.LogError(exception, "🛡️ [UNHANDLED EXCEPTION] Lỗi ngoại lệ hệ thống xảy ra tại '{Method} {Path}'. TraceId: {TraceId}",
            httpContext.Request.Method, httpContext.Request.Path, traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        // Trả phản hồi lỗi chung an toàn ra Client (OWASP API8: Security Misconfiguration - Chống rò rỉ StackTrace & SQL Schema)
        var problemDetails = new
        {
            success = false,
            statusCode = StatusCodes.Status500InternalServerError,
            error = "INTERNAL_SERVER_ERROR",
            message = "🛡️ [AN TOÀN HỆ THỐNG]: Đã xảy ra lỗi nội bộ trong quá trình xử lý yêu cầu. Thông tin chi tiết đã được bảo mật để đảm bảo an toàn an ninh mạng.",
            traceId = traceId,
            timestamp = DateTime.UtcNow
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
