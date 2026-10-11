using WebApi.V1_Vulnerable.Models;

namespace WebApi.V1_Vulnerable.Data
{
    public static class DbInitializer
    {
        public static void Initialize(V1DbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return; // Database đã được seed
            }

            // 1. Seed Roles
            var roles = new List<Role>
            {
                new Role { RoleName = "Admin", Description = "Quản trị viên toàn quyền hệ thống" },
                new Role { RoleName = "NhanVien", Description = "Nhân viên vận hành và kiểm tra" },
                new Role { RoleName = "NguoiDung", Description = "Khách hàng người dùng thông thường" }
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();

            // 2. Seed Users (V1: lưu mật khẩu plaintext để minh họa rủi ro an toàn)
            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    Password = "Admin@123",
                    FullName = "Hệ thống Quản Trị Viên",
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "staff1",
                    Password = "Staff@123",
                    FullName = "Nguyễn Văn Staff",
                    Role = "NhanVien",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "userA",
                    Password = "UserA@123",
                    FullName = "Võ Lê Nhật Hoàng",
                    Role = "NguoiDung",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "userB",
                    Password = "UserB@123",
                    FullName = "Trần Thị Bích",
                    Role = "NguoiDung",
                    CreatedAt = DateTime.UtcNow
                }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            var userA = context.Users.First(u => u.Username == "userA");
            var userB = context.Users.First(u => u.Username == "userB");

            // 3. Seed Orders (Id 1, 2, 5 thuộc userA; Id 3, 4 thuộc userB)
            var orders = new List<Order>
            {
                new Order
                {
                    OrderCode = "ORD-001",
                    UserId = userA.Id,
                    CustomerName = "Võ Lê Nhật Hoàng",
                    ProductName = "Laptop Dell XPS 15 OLED",
                    TotalAmount = 35000000,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Order
                {
                    OrderCode = "ORD-002",
                    UserId = userA.Id,
                    CustomerName = "Võ Lê Nhật Hoàng",
                    ProductName = "Bàn phím cơ Akko 3087 V2 Silent",
                    TotalAmount = 1550000,
                    Status = "Processing",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Order
                {
                    OrderCode = "ORD-003",
                    UserId = userB.Id,
                    CustomerName = "Trần Thị Bích",
                    ProductName = "Màn hình LG UltraGear 27GR75Q",
                    TotalAmount = 7200000,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Order
                {
                    OrderCode = "ORD-004",
                    UserId = userB.Id,
                    CustomerName = "Trần Thị Bích",
                    ProductName = "Chuột không dây Logitech MX Master 3S",
                    TotalAmount = 2490000,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Order
                {
                    OrderCode = "ORD-005",
                    UserId = userA.Id,
                    CustomerName = "Võ Lê Nhật Hoàng",
                    ProductName = "Tai nghe Sony WH-1000XM5",
                    TotalAmount = 8490000,
                    Status = "Shipping",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };
            context.Orders.AddRange(orders);
            context.SaveChanges();
        }
    }
}
