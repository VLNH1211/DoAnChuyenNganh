using System.Text;
using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext kết nối SQL Server LocalDB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Đăng ký các dịch vụ Dependency Injection (DI)
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISecurityLogService, SecurityLogService>();

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
});

// 4. Cấu hình Phân quyền (Authorization)
builder.Services.AddAuthorization();

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
// Bật Swagger cả ở môi trường Development và Production phục vụ chấm đồ án & test API trực tuyến
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechSec Gear API v1");
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");

// THỨ TỰ BẮT BUỘC: Xác thực (Authentication) -> Phân quyền (Authorization)
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<EcommerceSecurityApi.Middlewares.ActiveUserMiddleware>();

app.MapControllers();

app.Run();
