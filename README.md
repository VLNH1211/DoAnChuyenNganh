# TechSec Gear - Hệ Thống E-Commerce RESTful API & Trung Tâm Giám Sát An Ninh (SOC)

> **ĐỒ ÁN CHUYÊN NGÀNH AN TOÀN THÔNG TIN**  
> **Đề tài:** Xây dựng và kiểm thử cơ chế phòng thủ cho hệ thống RESTful API trên nền tảng ASP.NET Core  
> **Sinh viên thực hiện:** Võ Lê Nhật Hoàng  
> **MSSV:** 2387700017 | **Lớp:** 23DATA1  
> **Chuyên ngành:** An Toàn Thông Tin  

---

## 📌 1. Giới thiệu tổng quan
Dự án xây dựng một hệ thống Web API thương mại điện tử chuyên cung cấp thiết bị công nghệ và gaming gear cao cấp (**TechSec Gear**) trên nền tảng **ASP.NET Core 8.0**. Dự án tập trung vào việc hiện thực các cơ chế phòng thủ chuyên sâu theo tiêu chuẩn quốc tế **OWASP API Security Top 10 (2023)** và tích hợp **Trung tâm giám sát an ninh mạng (SOC)** theo thời gian thực.

### Công nghệ sử dụng:
- **Backend:** C# / ASP.NET Core 8.0 Web API, Entity Framework Core 8.0
- **Cơ sở dữ liệu:** Microsoft SQL Server / LocalDB
- **Xác thực & Mã hóa:** JWT (JSON Web Token - RFC 7519), BCrypt.Net-Next (băm mật khẩu với Salt an toàn)
- **Frontend Client:** Single Page Application (HTML5, Tailwind CSS, FontAwesome, JavaScript ES6)
- **Bảo mật & Giám sát:** ActiveUserMiddleware, SecurityLogService, SOC Dashboard
- **Kiểm thử API & Public:** Swagger UI (OpenAPI v1), Cloudflare Tunnel (HTTPS)

---

## 🛡️ 2. Các cơ chế an ninh & Phòng thủ trọng tâm

### 2.1. Phòng thủ lỗ hổng BOLA / IDOR (Broken Object Level Authorization - OWASP Top 1)
- **Nguy cơ:** Kẻ tấn công mang vai trò khách hàng cố tình thay đổi mã định danh đối tượng trên URL (ví dụ `GET /api/orders/{id}`) để truy cập dữ liệu đơn hàng của khách hàng khác.
- **Giải pháp phòng thủ:** 
  - Tại `OrdersController.cs`, hệ thống giải mã JWT Token để trích xuất `currentUserId`.
  - Tiến hành so sánh: chỉ cho phép xem nếu người dùng là `Admin`, `NhanVien`, hoặc chính là chủ sở hữu đơn hàng (`order.UserId == currentUserId`).
  - Khi phát hiện vi phạm: Máy chủ lập tức chặn đứng yêu cầu bằng mã lỗi **`HTTP 403 Forbidden`**, không để lộ bất kỳ thông tin nhạy cảm nào, đồng thời kích hoạt cảnh báo an ninh mức độ **Critical** vào cơ sở dữ liệu.

### 2.2. Xác thực & Phân quyền dựa trên vai trò (RBAC)
- **Hệ thống phân cấp 3 vai trò rõ ràng:**
  - `Admin`: Toàn quyền quản trị danh mục, sản phẩm, người dùng, xem thống kê doanh thu và quản lý Trung tâm SOC.
  - `NhanVien`: Quản lý kho hàng, kiểm duyệt và cập nhật trạng thái đơn hàng.
  - `KhachHang`: Mua sắm, quản lý giỏ hàng và theo dõi lịch sử đơn hàng cá nhân.
- Áp dụng bộ lọc `[Authorize(Roles = "...")]` nghiêm ngặt tại từng Controller / Action.

### 2.3. Kiểm soát trạng thái tài khoản thời gian thực (`ActiveUserMiddleware`)
- Ngay khi Quản trị viên thực hiện khóa tài khoản vi phạm trên giao diện SOC, Middleware sẽ kiểm tra trường `IsActive` của người dùng trước mọi yêu cầu HTTP. 
- Mọi thao tác tiếp theo của tài khoản đã bị khóa sẽ bị chặn đứng ngay lập tức với mã lỗi `HTTP 403`, vô hiệu hóa phiên làm việc mà không cần chờ JWT Token hết hạn.

### 2.4. Trung tâm giám sát an ninh (SOC Dashboard)
- Tự động ghi nhận nhật ký vi phạm an ninh: Sự cố BOLA/IDOR, lịch sử đăng nhập thành công, đăng ký tài khoản mới, sự cố dò quét mật khẩu.
- Hiển thị đầy đủ thông tin: Mốc thời gian thực (GMT+7), loại sự cố, mức độ nghiêm trọng, tài khoản vi phạm, IP truy cập và tài nguyên đích.
- Quản trị viên có thể bấm **Khóa tài khoản** hoặc **Mở khóa tài khoản** trực tiếp ngay trên giao diện bảng điều khiển.

---

## 🚀 3. Hướng dẫn cài đặt & Khởi chạy

### Yêu cầu hệ thống:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB hoặc SQL Server Express / Developer)

### Khởi chạy ứng dụng:
1. **Clone mã nguồn về máy:**
   ```bash
   git clone https://github.com/VLNH1211/DoAnChuyenNganh.git
   cd DoAnChuyenNganh
   ```

2. **Chạy ứng dụng bằng .NET CLI:**
   ```bash
   cd ĐACN/EcommerceSecurityApi
   dotnet run --urls "http://localhost:5141"
   ```

3. **Hoặc chạy tự động bằng file Script (Khuyên dùng):**
   - Nhấp đúp chuột vào file `start_public_web.bat` ở thư mục gốc.

4. **Truy cập hệ thống:**
   - **Giao diện Web Client & Bảng điều khiển SOC:** `http://localhost:5141`
   - **Giao diện Swagger API:** `http://localhost:5141/swagger`

---

## 🔑 4. Tài khoản mẫu để kiểm thử

| Tài khoản | Mật khẩu | Vai trò | Mục đích thử nghiệm |
|:---|:---|:---|:---|
| `admin` | `123456` | **Admin** | Quản trị toàn hệ thống, giám sát an ninh SOC, khóa/mở khóa tài khoản |
| `nhanvien` | `123456` | **NhanVien** | Duyệt đơn đặt hàng, quản lý kho sản phẩm |
| *(Tự tạo)* | *(Tự đặt)* | **KhachHang** | Đăng ký tài khoản khách hàng mới để trải nghiệm mua sắm và thử nghiệm tấn công BOLA |

---

## 🧪 5. Kịch bản kiểm thử phòng thủ BOLA / IDOR (Demo vấn đáp)

1. Đăng nhập tài khoản khách hàng **A** (ví dụ: `vinh1` - Nguyễn Văn A) và tiến hành đặt 1 đơn hàng (sinh ra mã đơn `#1003`).
2. Mở trình duyệt khác hoặc tab ẩn danh, đăng nhập tài khoản khách hàng **B** (ví dụ: `vlnh` - Võ Lê Nhật Hoàng).
3. Tại tài khoản khách hàng B, mở **F12 Console** và gửi yêu cầu xem trộm đơn hàng của khách hàng A:
   ```javascript
   viewSingleOrder(1003)
   ```
4. **Quan sát kết quả:**
   - Phía khách hàng B: Xuất hiện hộp thoại cảnh báo màu đỏ **`HTTP 403 FORBIDDEN - CHỐT CHẶN AN NINH WEB API KÍCH HOẠT`**. Kẻ tấn công không đọc được bất kỳ thông tin nào của đơn hàng #1003.
   - Phía Quản trị viên (Admin): Đăng nhập `admin`, mở tab **Giám sát An ninh (SOC)** sẽ thấy dòng log màu đỏ cảnh báo vi phạm `BOLA_IDOR_VIOLATION` ghi nhận hành vi của tài khoản B kèm nút **Khóa tài khoản**.
