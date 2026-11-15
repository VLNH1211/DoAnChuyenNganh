namespace EcommerceSecurityApi.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Chèn các Security Headers chuẩn OWASP trước khi gửi phản hồi về client
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // 1. Chống MIME-type Sniffing (OWASP API8:2023)
            headers["X-Content-Type-Options"] = "nosniff";

            // 2. Chống Clickjacking
            headers["X-Frame-Options"] = "DENY";

            // 3. Chống rò rỉ referrer sang tên miền bên thứ ba
            headers["Referrer-Policy"] = "no-referrer";

            // 4. Giới hạn các API phần cứng & tính năng nhạy cảm trên trình duyệt
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // 5. Chống XSS trên trình duyệt đời cũ
            headers["X-XSS-Protection"] = "1; mode=block";

            // 6. Ẩn thông tin Server & công nghệ backend
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            // 7. Chống lưu bộ nhớ đệm (Cache-Control) cho toàn bộ API nhạy cảm
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                headers["Pragma"] = "no-cache";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
