using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebApi.V2_Secure.Data;
using WebApi.V2_Secure.Middleware;
using WebApi.V2_Secure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database EF Core với SQL Server LocalDB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=SecurityLab_V2;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<V2DbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Đăng ký Services
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddSingleton<IRateLimitingService, RateLimitingService>();

// 3. Cấu hình JWT Bearer Authentication & Token Validation
var secretKey = builder.Configuration["JwtSettings:SecretKey"] 
    ?? "SuperSecretSecureKeyForHutechDoAnChuyenNganhSecurityLab20261010!";
var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "SecurityLab_V2";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "SecurityLab_Client";
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Phục vụ môi trường thực nghiệm localhost
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Chặn tuyệt đối token đã hết hạn
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // Bỏ qua cơ chế mặc định để trả về JSON chuẩn cho KB1
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";

            var auditLog = context.HttpContext.RequestServices.GetRequiredService<IAuditLogService>();
            var path = context.HttpContext.Request.Path.Value ?? "";
            var method = context.HttpContext.Request.Method;
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            await auditLog.LogAsync(
                "Anonymous",
                "None",
                path,
                method,
                401,
                "[KB1 Xác thực thất bại] Gọi API yêu cầu xác thực nhưng không cung cấp JWT Token hoặc Token không hợp lệ.",
                ip);

            var responseObj = new
            {
                success = false,
                statusCode = 401,
                message = "[V2 - KB1 Chặn 401 Unauthorized] Xác thực thất bại: Endpoint này yêu cầu cung cấp JWT Bearer Token hợp lệ.",
                error = "Unauthorized_MissingOrInvalidToken",
                endpoint = path,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";

            var auditLog = context.HttpContext.RequestServices.GetRequiredService<IAuditLogService>();
            var path = context.HttpContext.Request.Path.Value ?? "";
            var method = context.HttpContext.Request.Method;
            var user = context.HttpContext.User.Identity?.Name ?? "AuthenticatedUser";
            var role = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "UnknownRole";
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            await auditLog.LogAsync(
                user,
                role,
                path,
                method,
                403,
                $"[KB2 Phân quyền thất bại] User '{user}' có vai trò '{role}' bị từ chối do không đủ quyền hạn (Policy yêu cầu Admin).",
                ip);

            var responseObj = new
            {
                success = false,
                statusCode = 403,
                message = "[V2 - KB2 Chặn 403 Forbidden] Truy cập bị từ chối: Tài khoản không có vai trò phù hợp (Yêu cầu quyền Admin).",
                error = "Forbidden_InsufficientPrivilege",
                user = user,
                role = role,
                endpoint = path,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(responseObj));
        }
    };
});

// 4. Cấu hình Phân quyền (RBAC Policies)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Admin", "NhanVien"));
});

// 5. Đăng ký Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 6. Cấu hình Swagger UI có nút "Authorize" hỗ trợ JWT Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "SecurityLab API - V2_Secure (HUTECH ATTT)",
        Version = "v2",
        Description = "Hệ thống Thực nghiệm An toàn thông tin - Phiên bản V2 (Bảo mật: Đầy đủ 4 lớp kiểm soát KB1–KB4 + Audit Log)\n\n" +
                      "Đề tài: XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO ỨNG DỤNG WEB/API DỰA TRÊN XÁC THỰC, PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP\n\n" +
                      "Cổng chạy: http://localhost:5002\n\n" +
                      "Hướng dẫn: Đăng nhập tại `POST /api/auth/login` lấy token, sau đó bấm nút 'Authorize' phía trên bên phải và dán: `Bearer <token>`."
    });

    // Cấu hình nút Authorize cho Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Bearer token theo định dạng: Bearer {token}\nVí dụ: Bearer eyJhbGciOiJIUzI1NiIsIn...",
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

// 7. Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 8. Tự động khởi tạo Database và Seed dữ liệu mẫu khi khởi động
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<V2DbContext>();
        DbInitializer.Initialize(context);
        Console.WriteLine("[V2_Secure] Cơ sở dữ liệu SecurityLab_V2 đã được khởi tạo và nạp dữ liệu mẫu thành công.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "[V2_Secure] Lỗi khi khởi tạo cơ sở dữ liệu: {Message}", ex.Message);
    }
}

// 9. Cấu hình HTTP Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "WebApi.V2_Secure v2");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseMiddleware<SecurityAuditMiddleware>();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("==========================================================================");
Console.WriteLine(" WebApi.V2_Secure (Hardened) đang chạy tại: http://localhost:5002");
Console.WriteLine(" Swagger UI (kèm Authorize JWT): http://localhost:5002/swagger");
Console.WriteLine(" Sinh viên: Võ Lê Nhật Hoàng - MSSV: 2387700017 - HUTECH ATTT");
Console.WriteLine("==========================================================================");

app.Run("http://localhost:5002");
