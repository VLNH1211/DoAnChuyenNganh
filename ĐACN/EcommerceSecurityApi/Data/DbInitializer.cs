using EcommerceSecurityApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // 1. Khởi tạo cấu trúc database và các bảng nếu chưa có
        await context.Database.EnsureCreatedAsync();
        try
        {
            await context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = 'LastLoginAt') ALTER TABLE [Users] ADD [LastLoginAt] DATETIME2 NULL;");
        }
        catch
        {
            // Bỏ qua nếu bảng đã đủ cấu trúc hoặc quyền DDL hạn chế
        }

        // 2. Seed Roles
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new Role { Name = "Admin", Description = "Quản trị viên hệ thống công nghệ toàn quyền" },
                new Role { Name = "NhanVien", Description = "Kỹ thuật viên & Quản lý kho thiết bị công nghệ" },
                new Role { Name = "KhachHang", Description = "Khách hàng mua sắm đồ công nghệ trực tuyến" }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
        }

        // 3. Seed Users
        if (!await context.Users.AnyAsync())
        {
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
            var staffRole = await context.Roles.FirstAsync(r => r.Name == "NhanVien");
            var customerRole = await context.Roles.FirstAsync(r => r.Name == "KhachHang");

            // Seed Admin User
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@techsec.com",
                FullName = "Quản Trị Viên Hệ Thống (Admin)",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
            await context.UserRoles.AddAsync(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });

            // Seed Staff User
            var staffUser = new User
            {
                Username = "nhanvien",
                Email = "nhanvien@techsec.com",
                FullName = "Kỹ Thuật Viên / Thủ Kho (Staff)",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await context.Users.AddAsync(staffUser);
            await context.SaveChangesAsync();
            await context.UserRoles.AddAsync(new UserRole { UserId = staffUser.Id, RoleId = staffRole.Id });
            await context.SaveChangesAsync();
        }

        // Xóa 2 tài khoản khách hàng mẫu nếu còn tồn tại trong database theo yêu cầu
        var existingTestUsers = await context.Users
            .Where(u => u.Username == "khachhang" || u.Username == "otaku_vip")
            .ToListAsync();
        if (existingTestUsers.Count > 0)
        {
            var userIds = existingTestUsers.Select(u => u.Id).ToList();
            var ordersToDelete = await context.Orders.Where(o => userIds.Contains(o.UserId)).ToListAsync();
            context.Orders.RemoveRange(ordersToDelete);
            context.Users.RemoveRange(existingTestUsers);
            await context.SaveChangesAsync();
        }

        // 4. Seed Categories (5 Danh mục công nghệ)
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category { Name = "Laptop & Máy Tính", Description = "Laptops mỏng nhẹ, máy trạm workstation và PC gaming hiệu năng cao" },
                new Category { Name = "Điện Thoại & Tablet", Description = "Điện thoại flagship, máy tính bảng đồ họa và phụ kiện di động" },
                new Category { Name = "Âm Thanh & Tai Nghe", Description = "Tai nghe chống ồn chủ động, loa bluetooth hi-fi và thiết bị audio" },
                new Category { Name = "Bàn Phím & Gaming Gear", Description = "Bàn phím cơ custom, chuột gaming siêu nhẹ và phụ kiện eSports" },
                new Category { Name = "Linh Kiện & Màn Hình", Description = "Màn hình gaming OLED, card đồ họa GPU cao cấp và linh kiện PC" }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 5. Seed Products (10 Sản phẩm công nghệ chuẩn mực với ảnh HD đẹp nhất)
        if (!await context.Products.AnyAsync())
        {
            var catLaptops = await context.Categories.FirstAsync(c => c.Name.Contains("Laptop"));
            var catPhones = await context.Categories.FirstAsync(c => c.Name.Contains("Điện Thoại"));
            var catAudio = await context.Categories.FirstAsync(c => c.Name.Contains("Âm Thanh"));
            var catGear = await context.Categories.FirstAsync(c => c.Name.Contains("Bàn Phím"));
            var catComponents = await context.Categories.FirstAsync(c => c.Name.Contains("Linh Kiện"));

            var products = new List<Product>
            {
                new Product
                {
                    Name = "MacBook Pro 16 inch M3 Max (36GB / 1TB SSD)",
                    Brand = "Apple",
                    Price = 79990000m,
                    Stock = 15,
                    ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80",
                    Specification = "Chip M3 Max 16-Core CPU, 40-Core GPU • RAM 36GB Unified • 1TB SSD • Màn Liquid Retina XDR 120Hz",
                    Description = "Cỗ máy đồ họa tối thượng dành cho lập trình viên và nhà sáng tạo chuyên nghiệp. Hiệu năng đỉnh cao cùng thời lượng pin lên đến 22 giờ liên tục.",
                    CategoryId = catLaptops.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Product
                {
                    Name = "iPhone 16 Pro Max 256GB - Titan Tự Nhiên",
                    Brand = "Apple",
                    Price = 34990000m,
                    Stock = 35,
                    ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800&auto=format&fit=crop&q=80",
                    Specification = "Khung Titan Cấp 5 • Chip A18 Pro 3nm • Màn Super Retina XDR 6.9\" ProMotion 120Hz • Camera 48MP Zoom 5x",
                    Description = "Siêu phẩm flagship mới nhất với viền màn hình mỏng nhất lịch sử, nút Camera Control xúc giác chuyên nghiệp và khả năng quay video 4K 120fps Dolby Vision.",
                    CategoryId = catPhones.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-9)
                },
                new Product
                {
                    Name = "Tai Nghe Chống Ồn Cao Cấp Sony WH-1000XM5",
                    Brand = "Sony",
                    Price = 7490000m,
                    Stock = 40,
                    ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&auto=format&fit=crop&q=80",
                    Specification = "Chống ồn 2 bộ xử lý V1 + QN1 • 8 Micro Beamforming • Màng loa 30mm sợi Carbon • Pin 30 giờ",
                    Description = "Chuẩn mực tai nghe over-ear chống ồn hàng đầu thế giới với công nghệ Auto NC Optimizer tự động thích ứng với môi trường và định dạng âm thanh Hi-Res Audio Wireless LDAC.",
                    CategoryId = catAudio.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Product
                {
                    Name = "Bàn Phím Cơ Custom Không Dây Keychron Q1 Pro (Gasket Mount)",
                    Brand = "Keychron",
                    Price = 4890000m,
                    Stock = 50,
                    ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&auto=format&fit=crop&q=80",
                    Specification = "Vỏ nhôm CNC 100% • Double-Gasket Mount • Hotswap Switch K Pro • Bluetooth 5.1 & Type-C • Hỗ trợ QMK/VIA",
                    Description = "Bàn phím cơ cao cấp cho dân sành công nghệ. Cảm giác gõ đầm chắc, âm thanh trầm ấm tự nhiên, tương thích hoàn hảo giữa hệ điều hành macOS và Windows.",
                    CategoryId = catGear.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new Product
                {
                    Name = "Chuột Gaming Không Dây Logitech G Pro X Superlight 2",
                    Brand = "Logitech G",
                    Price = 3290000m,
                    Stock = 60,
                    ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=800&auto=format&fit=crop&q=80",
                    Specification = "Trọng lượng 60g • Cảm biến HERO 2 32.000 DPI • Switch Quang Học Lai LIGHTFORCE • Tần số 4000Hz Polling Rate",
                    Description = "Vũ khí thống trị các giải đấu Esports hàng đầu. Tốc độ phản hồi ánh sáng, feet chuột PTFE nguyên chất lướt êm ái và thời lượng pin 95 giờ.",
                    CategoryId = catGear.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new Product
                {
                    Name = "Màn Hình Gaming ASUS ROG Swift OLED PG27AQDM 27 inch 240Hz",
                    Brand = "ASUS ROG",
                    Price = 24990000m,
                    Stock = 20,
                    ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&auto=format&fit=crop&q=80",
                    Specification = "Tấm nền OLED 2K (2560x1440) • Tần số quét 240Hz • Phản hồi 0.03ms • 99% DCI-P3 • Tản nhiệt buồng hơi tùy chỉnh",
                    Description = "Màn hình gaming OLED đỉnh cao với độ tương phản vô cực, thời gian đáp ứng gần như tức thì và tản nhiệt độc quyền chống lưu ảnh hoàn hảo.",
                    CategoryId = catComponents.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Product
                {
                    Name = "Card Đồ Họa ASUS ROG Strix GeForce RTX 4090 OC Edition 24GB",
                    Brand = "ASUS ROG",
                    Price = 56990000m,
                    Stock = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1587202372775-e229f172b9d7?w=800&auto=format&fit=crop&q=80",
                    Specification = "Kiến trúc NVIDIA Ada Lovelace • 24GB GDDR6X 384-bit • Nhân Tensor Gen 4 & RT Gen 3 • Quạt Axial-tech làm mát tối ưu",
                    Description = "Card đồ họa mạnh mẽ nhất dành cho game thủ 4K Ray Tracing và huấn luyện mô hình Trí tuệ Nhân tạo (AI/Deep Learning) chuyên sâu.",
                    CategoryId = catComponents.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new Product
                {
                    Name = "Đồng Hồ Thông Minh Apple Watch Ultra 2 GPS + Cellular 49mm",
                    Brand = "Apple",
                    Price = 21490000m,
                    Stock = 25,
                    ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80",
                    Specification = "Vỏ Titan 49mm • Chống nước độ sâu 100m • Màn hình 3000 nits • Cảm biến GPS tần số kép L1+L5 • Pin 72 giờ",
                    Description = "Thiết kế hầm hố cho vận động viên và thám hiểm viên. Tính năng Thao tác Chạm hai lần (Double Tap) đột phá và còi báo động khẩn cấp 86dB.",
                    CategoryId = catPhones.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Product
                {
                    Name = "iPad Pro 11 inch M4 Wi-Fi 256GB - Space Black",
                    Brand = "Apple",
                    Price = 27990000m,
                    Stock = 30,
                    ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&auto=format&fit=crop&q=80",
                    Specification = "Độ mỏng kỷ lục 5.3mm • Chip Apple M4 thế hệ mới • Màn hình Ultra Retina XDR Tandem OLED • Hỗ trợ Apple Pencil Pro",
                    Description = "Sản phẩm mỏng nhất mà Apple từng chế tạo với sức mạnh đồ họa vượt bậc từ chip M4 và màn hình OLED xếp chồng cho độ sáng cực hạn.",
                    CategoryId = catPhones.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Product
                {
                    Name = "Loa Bluetooth Marshall Stanmore III Công Suất 80W",
                    Brand = "Marshall",
                    Price = 8990000m,
                    Stock = 25,
                    ImageUrl = "https://images.unsplash.com/photo-1545454675-3531b543be5d?w=800&auto=format&fit=crop&q=80",
                    Specification = "Công suất 80W RMS • Âm thanh Stereo trường rộng • Bluetooth 5.2 LE • Cổng kết nối RCA và 3.5mm AUX",
                    Description = "Phong cách Rock 'n' Roll cổ điển với các núm xoay điều khiển analog bằng đồng nguyên khối, âm trầm uy lực cùng công nghệ Dynamic Loudness độc quyền.",
                    CategoryId = catAudio.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        // 6. Seed Sample Order (Đơn hàng mẫu thuộc tài khoản Admin/Nội bộ phục vụ thử nghiệm BOLA)
        if (!await context.Orders.AnyAsync())
        {
            var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");
            var macbook = await context.Products.FirstAsync(p => p.Name.Contains("MacBook"));

            var sampleOrder = new Order
            {
                OrderCode = "ORD-SEC-001",
                UserId = adminUser.Id,
                CustomerName = "Đơn Hàng Mẫu Hệ Thống (Admin)",
                PhoneNumber = "0988889999",
                ShippingAddress = "Trụ sở TechSec Store, 123 Lê Lợi, Quận 1, TP.HCM",
                Notes = "Đơn hàng nội bộ công ty - Dùng để thử nghiệm bảo mật BOLA/IDOR",
                Status = "Processing",
                OrderDate = DateTime.UtcNow.AddDays(-1),
                TotalAmount = macbook.Price,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = macbook.Id,
                        Quantity = 1,
                        UnitPrice = macbook.Price,
                        TotalPrice = macbook.Price
                    }
                }
            };

            await context.Orders.AddAsync(sampleOrder);
            await context.SaveChangesAsync();
        }
    }
}
