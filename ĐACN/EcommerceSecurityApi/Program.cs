using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.Helpers;
using EcommerceSecurityApi.Middlewares;
using EcommerceSecurityApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 0. Hardening Kestrel Web Server (Tắt Server header tiết lộ công nghệ - OWASP API8)
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// 1. Cấu hình DbContext kết nối SQL Server LocalDB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Đăng ký các dịch vụ Dependency Injection (DI)
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISecurityLogService, SecurityLogService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

// Đăng ký Global Exception Handler (ASP.NET Core 8.0)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Cấu hình xác thực JWT Bearer
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Chưa cấu hình JWT Secret Key");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Chặn ngay khi token hết hạn, không có độ trễ
    };

    // Tự động bắt vi phạm BFLA (Broken Function Level Authorization) và ghi nhật ký SOC
    options.Events = new JwtBearerEvents
    {
        OnForbidden = async context =>
        {
            var httpContext = context.HttpContext;
            var clientIp = ClientInfoHelper.GetClientIpAddress(httpContext);
            var path = httpContext.Request.Path.Value ?? string.Empty;
            var method = httpContext.Request.Method;

            int? userId = null;
            var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int uid))
            {
                userId = uid;
            }
            var username = httpContext.User.Identity?.Name ?? (userId.HasValue ? $"User_{userId}" : "AuthenticatedUser");
            var userRoles = httpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            var rolesStr = userRoles.Count > 0 ? string.Join(", ", userRoles) : "NoRole";

            try
            {
                var securityLogService = httpContext.RequestServices.GetService<ISecurityLogService>();
                if (securityLogService != null)
                {
                    await securityLogService.LogIncidentAsync(
                        incidentType: "BFLA_PRIVILEGE_ESCALATION",
                        severity: "Critical",
                        description: $"Phát hiện hành vi cố tình leo thang đặc quyền trái phép (BFLA/OWASP API5). Tài khoản '{username}' (Vai trò: [{rolesStr}]) cố gọi API bảo vệ '{method} {path}'.",
                        targetResource: $"{method} {path}",
                        userId: userId,
                        username: username,
                        ipAddress: clientIp
                    );
                }
            }
            catch
            {
                // Bỏ qua lỗi ghi log để không làm treo pipeline
            }

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                httpContext.Response.ContentType = "application/json";

                var responseObj = new
                {
                    success = false,
                    statusCode = StatusCodes.Status403Forbidden,
                    error = "BFLA_FORBIDDEN",
                    message = "🛡️ [PHÒNG THỦ BFLA KÍCH HOẠT]: Bạn không có đặc quyền truy cập tài nguyên này (Broken Function Level Authorization)!",
                    detail = $"Tài khoản '{username}' với vai trò [{rolesStr}] không được cấp phép thực thi hành động '{method} {path}'. Hành vi đã được ghi lại trên Hệ thống Giám sát An ninh (SOC).",
                    timestamp = DateTime.UtcNow
                };

                await httpContext.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(responseObj));
            }
        }
    };
});

// 4. Cấu hình Phân quyền (Authorization)
builder.Services.AddAuthorization();

// 4.1 Cấu hình Chống Brute-force & Spam API bằng Rate Limiting (OWASP API4:2023)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy "LoginLimit": Tối đa 5 requests / 60s theo IP client cho POST /api/auth/login
    options.AddPolicy("LoginLimit", httpContext =>
    {
        var clientIp = ClientInfoHelper.GetClientIpAddress(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"login_{clientIp}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            });
    });

    // Policy "OrderLimit": Tối đa 10 requests / 60s theo User ID hoặc IP cho POST /api/orders
    options.AddPolicy("OrderLimit", httpContext =>
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var partitionKey = !string.IsNullOrEmpty(userId)
            ? $"order_user_{userId}"
            : $"order_ip_{ClientInfoHelper.GetClientIpAddress(httpContext)}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            });
    });

    // Global Limiter: 100 requests / 60s theo IP để phòng chống DDoS/crawler spam
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = ClientInfoHelper.GetClientIpAddress(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"global_{clientIp}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            });
    });

    // Xử lý khi vượt ngưỡng: Trả về HTTP 429 JSON + Bắn cảnh báo lên SOC Dashboard
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/json";

        int retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)retryAfter.TotalSeconds);
        }

        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

        var path = httpContext.Request.Path.Value ?? string.Empty;
        var clientIp = ClientInfoHelper.GetClientIpAddress(httpContext);
        var isLogin = path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase);

        var incidentType = "RATE_LIMIT_EXCEEDED";
        var severity = isLogin ? "Critical" : "Warning";
        var description = isLogin
            ? $"Phát hiện tấn công dò quét / Brute-force mật khẩu đăng nhập từ IP '{clientIp}'. Đã kích hoạt cơ chế phòng thủ Rate Limiting (5 lần/phút)."
            : $"Phát hiện hành vi gửi yêu cầu dồn dập (Spam/DDoS) vượt ngưỡng cho phép tới tài nguyên '{httpContext.Request.Method} {path}' từ IP '{clientIp}'.";

        int? userId = null;
        var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out int uid))
        {
            userId = uid;
        }
        var username = httpContext.User.Identity?.Name;

        try
        {
            var securityLogService = httpContext.RequestServices.GetService<ISecurityLogService>();
            if (securityLogService != null)
            {
                await securityLogService.LogIncidentAsync(
                    incidentType: incidentType,
                    severity: severity,
                    description: description,
                    targetResource: $"{httpContext.Request.Method} {path}",
                    userId: userId,
                    username: username,
                    ipAddress: clientIp
                );
            }
        }
        catch
        {
            // Bỏ qua lỗi ghi log để không làm đứt gãy response trả về
        }

        var responseObj = new
        {
            success = false,
            statusCode = StatusCodes.Status429TooManyRequests,
            error = "RATE_LIMIT_EXCEEDED",
            message = "🛡️ [PHÒNG THỦ RATE LIMIT KÍCH HOẠT]: Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau.",
            retryAfterSeconds = retryAfterSeconds,
            timestamp = DateTime.UtcNow
        };

        await httpContext.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(responseObj), cancellationToken);
    };
});

// 5. Cấu hình Controllers
builder.Services.AddControllers();

// 6. Cấu hình CORS (Cho phép Web Client gọi API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 7. Cấu hình Swagger UI kèm Bearer Token Authorization
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TechSec Gear - E-Commerce Security RESTful API",
        Version = "v1",
        Description = "Hệ thống Web API Bán Đồ Công Nghệ & Gaming Gear - Phòng Thủ An Ninh Mạng, Xác Thực JWT Bearer, Phân Quyền RBAC & Chống BOLA/IDOR (ASP.NET Core 8.0)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập Token theo cú pháp: Bearer {token}\nVí dụ: Bearer eyJhbGciOi...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 8. Tự động khởi tạo Database và nạp sẵn 3 Role + 3 Tài khoản mẫu (Data Seeding)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbInitializer.InitializeAsync(context);
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation(">>> Khởi tạo cơ sở dữ liệu và nạp dữ liệu mẫu (Seeding) thành công! <<<");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo cơ sở dữ liệu.");
    }
}

// 9. Cấu hình HTTP Request Pipeline
// Bắt ngoại lệ toàn cục đầu tiên trong pipeline (OWASP API8: Chống rò rỉ stack trace / thông tin hệ thống)
app.UseExceptionHandler();

// Chèn các Security Headers phòng thủ vào toàn bộ response
app.UseMiddleware<SecurityHeadersMiddleware>();

// Bật Swagger cả ở môi trường Development và Production phục vụ chấm đồ án & test API trực tuyến
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechSec Gear API v1");
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");

// THỨ TỰ BẮT BUỘC: Xác thực (Authentication) -> Phân quyền (Authorization) -> Rate Limiting -> Kiểm tra tài khoản
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<EcommerceSecurityApi.Middlewares.ActiveUserMiddleware>();

app.MapControllers();

app.Run();

// Cho phép WebApplicationFactory<Program> truy cập khi chạy kiểm thử xUnit (Phase 5)
public partial class Program { }
