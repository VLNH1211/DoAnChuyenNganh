using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WebApi.V1_Vulnerable.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database EF Core với SQL Server LocalDB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=SecurityLab_V1;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<V1DbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Đăng ký Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. Cấu hình Swagger UI cho bản V1
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SecurityLab API - V1_Vulnerable (HUTECH ATTT)",
        Version = "v1",
        Description = "Hệ thống Thực nghiệm An toàn thông tin - Phiên bản V1 (Baseline: Cố ý thiếu các cơ chế bảo mật KB1–KB4)\n\n" +
                      "Sinh viên thực hiện: Võ Lê Nhật Hoàng (MSSV: 2387700017 - Lớp: 23DATA1)\n" +
                      "Đề tài: XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO ỨNG DỤNG WEB/API DỰA TRÊN XÁC THỰC, PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP\n\n" +
                      "Cổng chạy: http://localhost:5001",
        Contact = new OpenApiContact
        {
            Name = "Võ Lê Nhật Hoàng",
            Email = "2387700017@student.hutech.edu.vn"
        }
    });
});

// 4. Cấu hình CORS cho phép kiểm thử linh hoạt
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

// 5. Tự động khởi tạo Database và Seed dữ liệu mẫu khi khởi động
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<V1DbContext>();
        DbInitializer.Initialize(context);
        Console.WriteLine("[V1_Vulnerable] Cơ sở dữ liệu SecurityLab_V1 đã được khởi tạo và nạp dữ liệu mẫu thành công.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "[V1_Vulnerable] Lỗi khi khởi tạo cơ sở dữ liệu: {Message}", ex.Message);
    }
}

// 6. Swagger UI luôn khả dụng để phục vụ demo và kiểm thử
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi.V1_Vulnerable v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

Console.WriteLine("==========================================================================");
Console.WriteLine(" WebApi.V1_Vulnerable (Baseline) đang chạy tại: http://localhost:5001");
Console.WriteLine(" Swagger UI: http://localhost:5001/swagger");
Console.WriteLine(" Sinh viên: Võ Lê Nhật Hoàng - MSSV: 2387700017 - HUTECH ATTT");
Console.WriteLine("==========================================================================");

app.Run();
