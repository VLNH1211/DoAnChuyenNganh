using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.DTOs.Orders;
using EcommerceSecurityApi.Entities;
using EcommerceSecurityApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcommerceSecurityApi.Tests;

public class SecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateToken(int userId, string username, IList<string> roles)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = new User
        {
            Id = userId,
            Username = username,
            Email = $"{username}@test.com"
        };
        return tokenService.GenerateJwtToken(user, roles, out _);
    }

    // =========================================================================
    // TEST CASE 1: OWASP API1:2023 - BOLA / IDOR Defense
    // Người dùng A cố tình đọc trộm đơn hàng của người dùng B -> Chặn 403 & Ghi SOC Log
    // =========================================================================
    [Fact]
    public async Task Test1_BOLA_GetOtherUserOrder_Returns403_AndCreatesCriticalSecurityLog()
    {
        // Arrange: Customer 1 (Id: 10) cố tình đọc đơn hàng #999 của Customer 2 (Id: 20)
        var token = GenerateToken(10, "customer1", new[] { "KhachHang" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/orders/999");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BOLA_SECURITY_VIOLATION", body);

        // Kiểm tra log SOC được ghi nhận
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bolaLog = await db.SecurityLogs.FirstOrDefaultAsync(l => l.IncidentType == "BOLA_IDOR_VIOLATION" && l.UserId == 10);
        Assert.NotNull(bolaLog);
        Assert.Equal("Critical", bolaLog.Severity);
    }

    // =========================================================================
    // TEST CASE 2: OWASP API5:2023 - BFLA Defense (Khách hàng gọi API Quản trị viên)
    // Tài khoản KhachHang gọi API Admin -> Chặn 403 & Tự động ghi SOC Log
    // =========================================================================
    [Fact]
    public async Task Test2_BFLA_CustomerAccessAdminEndpoint_Returns403_AndCreatesSecurityLog()
    {
        // Arrange: Customer 1 cố tình gọi API chỉ dành riêng cho Admin
        var token = GenerateToken(10, "customer1", new[] { "KhachHang" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/auth/admin-only");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BFLA_FORBIDDEN", body);

        // Kiểm tra log SOC được ghi nhận
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bflaLog = await db.SecurityLogs.FirstOrDefaultAsync(l => l.IncidentType == "BFLA_PRIVILEGE_ESCALATION" && l.UserId == 10);
        Assert.NotNull(bflaLog);
        Assert.Equal("Critical", bflaLog.Severity);
    }

    // =========================================================================
    // TEST CASE 3: OWASP API5:2023 - BFLA Defense (Nhân viên xóa sản phẩm)
    // Tài khoản NhanVien gọi API DELETE sản phẩm (chỉ Admin) -> Chặn 403 & Ghi SOC Log
    // =========================================================================
    [Fact]
    public async Task Test3_BFLA_StaffDeleteProduct_Returns403_AndCreatesSecurityLog()
    {
        // Arrange: Staff cố tình gọi DELETE /api/products/1
        var token = GenerateToken(40, "staff_tester", new[] { "NhanVien" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/api/products/1");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("BFLA_FORBIDDEN", body);

        // Kiểm tra log SOC
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bflaLog = await db.SecurityLogs.FirstOrDefaultAsync(l => l.IncidentType == "BFLA_PRIVILEGE_ESCALATION" && l.UserId == 40);
        Assert.NotNull(bflaLog);
    }

    // =========================================================================
    // TEST CASE 4: OWASP API3:2023 - Chống Price Tampering & Mass Assignment
    // Server tự lấy đơn giá từ DB, bỏ qua hoàn toàn giá client gửi lên
    // =========================================================================
    [Fact]
    public async Task Test4_DataTampering_CreateOrder_UsesServerProductPrice_IgnoresTamperedInput()
    {
        // Arrange: Customer 1 mua 2 món Product 1 (giá niêm yết trong DB là 3.000.000đ)
        var token = GenerateToken(10, "customer1", new[] { "KhachHang" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orderPayload = new CreateOrderDto
        {
            CustomerName = "Khách Hàng 1",
            PhoneNumber = "0901234567",
            ShippingAddress = "123 Đường Công Nghệ, TP.HCM",
            Notes = "Giao giờ hành chính",
            Items = new List<CreateOrderItemRequestDto>
            {
                new CreateOrderItemRequestDto
                {
                    ProductId = 1,
                    Quantity = 2
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", orderPayload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var totalAmount = json.GetProperty("order").GetProperty("totalAmount").GetDecimal();

        // Giá phải là 2 * 3.000.000 = 6.000.000đ từ DB
        Assert.Equal(6000000m, totalAmount);
    }

    // =========================================================================
    // TEST CASE 5: Kiểm tra đình chỉ tài khoản tức thì (Account Suspension)
    // Tài khoản có IsActive = false bị ActiveUserMiddleware chặn ngay với mã 403
    // =========================================================================
    [Fact]
    public async Task Test5_AccountSuspension_LockedUserWithValidToken_BlockedImmediatelyWith403()
    {
        // Arrange: locked_user (Id: 30) sở hữu một token hợp lệ
        var token = GenerateToken(30, "locked_user", new[] { "KhachHang" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ACCOUNT_SUSPENDED", body);
    }

    // =========================================================================
    // TEST CASE 6: OWASP API2:2023 - Thu hồi JWT Token khi Đăng xuất (Token Revocation)
    // Sau khi gọi POST /api/auth/logout, dùng lại Token đó sẽ nhận mã 401 Unauthorized
    // =========================================================================
    [Fact]
    public async Task Test6_TokenRevocation_AfterLogout_TokenIsRejectedWith401()
    {
        // Arrange: Customer 1 tạo token và gọi logout
        var token = GenerateToken(10, "customer1", new[] { "KhachHang" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Gọi Logout để đưa Token vào Blacklist
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // 2. Tái sử dụng chính Token đó để gọi API yêu cầu xác thực
        var reuseResponse = await _client.GetAsync("/api/auth/me");

        // Assert: Bị ActiveUserMiddleware chặn ngay lập tức
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        var body = await reuseResponse.Content.ReadAsStringAsync();
        Assert.Contains("TOKEN_REVOKED", body);
    }

    // =========================================================================
    // TEST CASE 7: OWASP API4:2023 - Chống Brute-Force bằng Rate Limiting (429)
    // Gửi liên tiếp 6 request login sai từ cùng 1 IP -> Request thứ 6 nhận HTTP 429
    // =========================================================================
    [Fact]
    public async Task Test7_RateLimiting_LoginBruteForce_ExceedingThreshold_Returns429()
    {
        // Arrange: Chuẩn bị 1 client riêng với IP riêng biệt để kiểm tra rate limit
        var testIp = "192.168.100.88";
        using var rateClient = _factory.CreateClient();
        rateClient.DefaultRequestHeaders.Add("X-Forwarded-For", testIp);

        HttpResponseMessage? lastResponse = null;

        // Act: Gửi 6 request login sai liên tiếp
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await rateClient.PostAsJsonAsync("/api/auth/login", new
            {
                username = "admin",
                password = "wrong_password_attack"
            });
        }

        // Assert: Request thứ 6 phải bị chặn bởi HTTP 429 Too Many Requests
        Assert.NotNull(lastResponse);
        Assert.Equal((HttpStatusCode)429, lastResponse.StatusCode);

        var body = await lastResponse.Content.ReadAsStringAsync();
        Assert.Contains("RATE_LIMIT_EXCEEDED", body);

        // Kiểm tra log SOC được ghi nhận
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rateLog = await db.SecurityLogs.FirstOrDefaultAsync(l => l.IncidentType == "RATE_LIMIT_EXCEEDED" && l.IpAddress == testIp);
        Assert.NotNull(rateLog);
        Assert.Equal("Critical", rateLog.Severity);
    }

    // =========================================================================
    // TEST CASE 8: OWASP API8:2023 - Kiểm tra Security Headers phòng thủ
    // Response phải chứa đầy đủ các header phòng chống tấn công & ẩn Server header
    // =========================================================================
    [Fact]
    public async Task Test8_SecurityHeaders_ApiResponse_ContainsDefensiveHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert: Các header phòng vệ chuẩn OWASP phải có mặt
        Assert.True(response.Headers.Contains("X-Content-Type-Options"), "Thiếu header X-Content-Type-Options");
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-Frame-Options"), "Thiếu header X-Frame-Options");
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());

        Assert.True(response.Headers.Contains("Referrer-Policy"), "Thiếu header Referrer-Policy");
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").First());

        // Header Server không được phép tiết lộ phiên bản/công nghệ
        Assert.False(response.Headers.Contains("Server"), "Header Server vẫn còn tồn tại (tiết lộ công nghệ Kestrel)");
    }
}
