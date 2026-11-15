using Microsoft.AspNetCore.Http;

namespace EcommerceSecurityApi.Helpers;

public static class ClientInfoHelper
{
    /// <summary>
    /// Trích xuất địa chỉ IP thực tế của client, hỗ trợ Cloudflare Tunnel (CF-Connecting-IP) và Reverse Proxy (X-Forwarded-For)
    /// </summary>
    public static string GetClientIpAddress(HttpContext httpContext)
    {
        // 1. Kiểm tra header của Cloudflare Tunnel
        if (httpContext.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) && !string.IsNullOrWhiteSpace(cfIp))
        {
            return cfIp.ToString().Trim();
        }

        // 2. Kiểm tra header của Reverse Proxy tiêu chuẩn
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedIp) && !string.IsNullOrWhiteSpace(forwardedIp))
        {
            var ipList = forwardedIp.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (ipList.Length > 0)
            {
                return ipList[0];
            }
        }

        // 3. Fallback về IP kết nối trực tiếp
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}
