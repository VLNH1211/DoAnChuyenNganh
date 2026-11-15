using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceSecurityApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestSecurityDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. Gỡ bỏ cấu hình DbContext SQL Server thực tế
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 2. Thay thế bằng InMemory Database hoàn toàn cô lập, không ảnh hưởng đến CSDL thật
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            // 3. Khởi tạo dữ liệu thực nghiệm mẫu cho môi trường kiểm thử
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.EnsureCreated();
            SeedTestData(context);
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(AppDbContext context)
    {
        // Đảm bảo Roles tồn tại
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { Id = 1, Name = "Admin", Description = "Quản trị viên" },
                new Role { Id = 2, Name = "NhanVien", Description = "Nhân viên" },
                new Role { Id = 3, Name = "KhachHang", Description = "Khách hàng" }
            );
            context.SaveChanges();
        }

        // Tạo người dùng mẫu phục vụ kịch bản an ninh
        if (!context.Users.Any(u => u.Username == "customer1"))
        {
            var customerRole = context.Roles.First(r => r.Name == "KhachHang");
            var staffRole = context.Roles.First(r => r.Name == "NhanVien");
            var adminRole = context.Roles.First(r => r.Name == "Admin");

            // Customer 1 (Chủ sở hữu đơn hàng A)
            var c1 = new User
            {
                Id = 10,
                Username = "customer1",
                Email = "customer1@test.com",
                FullName = "Khách Hàng 1",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(c1);
            context.SaveChanges();
            context.UserRoles.Add(new UserRole { UserId = c1.Id, RoleId = customerRole.Id });

            // Customer 2 (Nạn nhân trong kịch bản BOLA/IDOR)
            var c2 = new User
            {
                Id = 20,
                Username = "customer2",
                Email = "customer2@test.com",
                FullName = "Khách Hàng 2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(c2);
            context.SaveChanges();
            context.UserRoles.Add(new UserRole { UserId = c2.Id, RoleId = customerRole.Id });

            // User bị vô hiệu hóa (Bị khóa trong kịch bản Account Suspension)
            var lockedUser = new User
            {
                Id = 30,
                Username = "locked_user",
                Email = "locked@test.com",
                FullName = "Tài Khoản Bị Khóa",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                IsActive = false, // Bị đình chỉ
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(lockedUser);
            context.SaveChanges();
            context.UserRoles.Add(new UserRole { UserId = lockedUser.Id, RoleId = customerRole.Id });

            // Staff (Nhân viên)
            var staff = new User
            {
                Id = 40,
                Username = "staff_tester",
                Email = "staff@test.com",
                FullName = "Nhân Viên Kiểm Thử",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(staff);
            context.SaveChanges();
            context.UserRoles.Add(new UserRole { UserId = staff.Id, RoleId = staffRole.Id });

            // Sản phẩm mẫu
            var product = new Product
            {
                Id = 1,
                Name = "Chuột Gaming Logitech G Pro X Superlight",
                Brand = "Logitech",
                Price = 3000000m,
                Stock = 50,
                Description = "Chuột gaming không dây",
                CategoryId = 1,
                CreatedAt = DateTime.UtcNow
            };
            context.Products.Add(product);
            context.SaveChanges();

            // Đơn hàng mẫu của Customer 2 (Đơn #999)
            var orderOfC2 = new Order
            {
                Id = 999,
                OrderCode = "ORD-TEST-999",
                UserId = c2.Id,
                CustomerName = c2.FullName,
                PhoneNumber = "0912345678",
                ShippingAddress = "Hà Nội",
                TotalAmount = 3000000m,
                Status = "Pending",
                OrderDate = DateTime.UtcNow
            };
            context.Orders.Add(orderOfC2);
            context.SaveChanges();

            context.OrderItems.Add(new OrderItem
            {
                OrderId = orderOfC2.Id,
                ProductId = product.Id,
                Quantity = 1,
                UnitPrice = 3000000m,
                TotalPrice = 3000000m
            });
            context.SaveChanges();
        }
    }
}
