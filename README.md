# TechSec Gear - Hệ Thống E-Commerce RESTful API & Trung Tâm Giám Sát An Ninh (SOC)

> **ĐỒ ÁN CHUYÊN NGÀNH AN TOÀN THÔNG TIN**  
> **Đề tài:** Xây dựng và kiểm thử cơ chế phòng thủ cho hệ thống RESTful API trên nền tảng ASP.NET Core  
> **Sinh viên thực hiện:** Võ Lê Nhật Hoàng  
> **MSSV:** 2387700017 | **Lớp:** 23DATA1  
> **Chuyên ngành:** An Toàn Thông Tin  

---

## 📌 1. Giới thiệu tổng quan
Dự án xây dựng một hệ thống Web API thương mại điện tử chuyên cung cấp thiết bị công nghệ và gaming gear cao cấp (**TechSec Gear**) trên nền tảng **ASP.NET Core 8.0**. Dự án tập trung vào việc hiện thực các cơ chế phòng thủ chuyên sâu theo tiêu chuẩn an ninh quốc tế **OWASP API Security Top 10 (2023)** và tích hợp **Trung tâm giám sát an ninh mạng (SOC)** theo thời gian thực.

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
  - Tiến hành so sánh quyền sở hữu đối tượng ở tầng máy chủ: chỉ cho phép xem nếu người dùng là `Admin`, `NhanVien`, hoặc chính là chủ sở hữu đơn hàng (`order.UserId == currentUserId`).
  - Khi phát hiện vi phạm: Máy chủ lập tức chặn đứng yêu cầu bằng mã lỗi **`HTTP 403 Forbidden`**, không để lộ bất kỳ thông tin nhạy cảm nào, đồng thời kích hoạt cảnh báo an ninh mức độ **Critical** vào cơ sở dữ liệu.

### 2.2. Phòng thủ leo thang đặc quyền & Lỗ hổng phân quyền chức năng (BFLA - OWASP Top 5)
- **Hệ thống phân cấp 3 vai trò rõ ràng (RBAC):**
  - `Admin`: Toàn quyền quản trị danh mục, sản phẩm, người dùng, xem thống kê doanh thu và quản lý Trung tâm SOC.
  - `NhanVien`: Quản lý kho hàng, kiểm duyệt và cập nhật trạng thái đơn hàng.
  - `KhachHang`: Mua sắm, quản lý giỏ hàng và theo dõi lịch sử đơn hàng cá nhân.
- Áp dụng bộ lọc `[Authorize(Roles = "...")]` nghiêm ngặt tại từng Controller / Action phía máy chủ, ngăn chặn khách hàng gọi trái phép các API quản trị.

### 2.3. Phòng thủ xác thực (Broken Authentication - OWASP Top 2)
- Kiểm tra tính hợp lệ và thời hạn (`exp`) của chuỗi JSON Web Token trước mọi yêu cầu truy cập tài nguyên bảo vệ.
- Mật khẩu người dùng được băm một chiều bằng thuật toán BCrypt an toàn, loại bỏ hoàn toàn nguy cơ lưu mật khẩu dạng văn bản thuần.

### 2.4. Kiểm soát trạng thái tài khoản thời gian thực (`ActiveUserMiddleware`)
- Ngay khi Quản trị viên thực hiện khóa tài khoản vi phạm trên giao diện SOC, Middleware sẽ kiểm tra trường `IsActive` của người dùng trước mọi yêu cầu HTTP. 
- Mọi thao tác tiếp theo của tài khoản đã bị khóa sẽ bị chặn đứng ngay lập tức với mã lỗi `HTTP 403`, vô hiệu hóa phiên làm việc theo thời gian thực mà không cần chờ JWT Token hết hạn.

### 2.5. Phòng thủ chống thao túng giá tiền & dữ liệu (Data Tampering / Mass Assignment)
- Tách biệt hoàn toàn DTO đầu vào (`CreateOrderDto`) với thực thể cơ sở dữ liệu.
- Giá bán sản phẩm và tổng giá trị đơn hàng được tính toán độc lập ở phía máy chủ dựa trên dữ liệu niêm yết trong cơ sở dữ liệu, triệt tiêu nguy cơ kẻ gian sửa đổi giá tiền trong gói tin gửi lên.

### 2.6. Trung tâm giám sát an ninh (SOC Dashboard)
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
| *(Tự tạo)* | *(Tự đặt)* | **KhachHang** | Đăng ký tài khoản khách hàng mới để trải nghiệm mua sắm và thử nghiệm các kịch bản tấn công |

---

## 🧪 5. Các kịch bản kiểm thử phòng thủ an ninh (Kịch bản Demo vấn đáp)

Hệ thống được thiết kế với 5 kịch bản thực nghiệm đối chứng để chứng minh năng lực phòng thủ:

### 🔹 Kịch bản 1: Phòng thủ lỗ hổng BOLA / IDOR (OWASP API Top 1)
- **Tình huống:** Tài khoản khách hàng B (`vlnh` - Võ Lê Nhật Hoàng) muốn xem lén thông tin đơn hàng và số điện thoại của khách hàng A (`vinh1` - Nguyễn Văn A, sở hữu đơn `#1003`).
- **Thao tác thực hiện:**
  1. Đăng nhập tài khoản `vlnh`.
  2. Mở Console trình duyệt (F12) và gửi yêu cầu truy xuất trực tiếp đơn hàng #1003:
     ```javascript
     viewSingleOrder(1003)
     ```
- **Kết quả phòng thủ:**
  - Máy chủ trả về mã lỗi **`HTTP 403 Forbidden`** kèm thông báo: *"Bạn không có quyền truy cập vào đơn hàng của khách hàng khác!"*.
  - Bảng điều khiển SOC của Quản trị viên lập tức ghi nhận dòng log màu đỏ **`Critical`** với mã sự cố `BOLA_IDOR_VIOLATION`.

---

### 🔹 Kịch bản 2: Phòng thủ leo thang đặc quyền / BFLA (OWASP API Top 5)
- **Tình huống:** Tài khoản mang vai trò `KhachHang` gửi yêu cầu can thiệp vào các API đặc quyền của Quản trị viên nhằm đọc lén danh sách tài khoản hoặc xem toàn bộ nhật ký an ninh hệ thống.
- **Thao tác thực hiện:**
  1. Dùng tài khoản khách hàng gửi yêu cầu GET qua Console hoặc Swagger UI:
     ```javascript
     fetch('/api/admin/users', {
       headers: { 'Authorization': 'Bearer ' + currentAuthData.token }
     }).then(res => console.log('HTTP Status:', res.status));
     ```
- **Kết quả phòng thủ:**
  - Bộ lọc `[Authorize(Roles = "Admin")]` phía máy chủ kiểm tra danh sách Roles trong JWT Claims.
  - Phản hồi trả về là **`HTTP 403 Forbidden`**, không trả về bất kỳ dữ liệu người dùng nào.

---

### 🔹 Kịch bản 3: Phòng thủ xác thực thiếu hoặc giả mạo Token (OWASP API Top 2)
- **Tình huống:** Kẻ tấn công không đăng nhập nhưng cố tình gửi yêu cầu lấy danh sách đơn hàng cá nhân hoặc đặt hàng trái phép.
- **Thao tác thực hiện:**
  1. Gửi yêu cầu HTTP không đính kèm Header `Authorization`:
     ```javascript
     fetch('/api/orders/my-orders').then(res => console.log('HTTP Status:', res.status));
     ```
- **Kết quả phòng thủ:**
  - `JwtBearerMiddleware` phát hiện yêu cầu thiếu chuỗi định danh Bearer Token hợp lệ.
  - Máy chủ trả về mã lỗi **`HTTP 401 Unauthorized`** ngay tại tầng Middleware, không cho phép yêu cầu đi vào Controller.

---

### 🔹 Kịch bản 4: Phòng thủ chủ động & Vô hiệu hóa tài khoản vi phạm qua Middleware
- **Tình huống:** Kẻ tấn công vừa thực hiện hành vi vi phạm an ninh và vẫn đang giữ JWT Token còn hạn (thời hạn 120 phút).
- **Thao tác thực hiện:**
  1. Quản trị viên mở Trung tâm Giám sát SOC (`admin`), kiểm tra nhật ký vi phạm và bấm nút **"Khóa tài khoản"** của kẻ tấn công.
  2. Kẻ tấn công quay lại giao diện, tiếp tục gửi yêu cầu đặt hàng hoặc truy cập API:
     ```javascript
     fetchOrders();
     ```
- **Kết quả phòng thủ:**
  - `ActiveUserMiddleware` kiểm tra trường `IsActive` trong cơ sở dữ liệu thời gian thực.
  - Mọi yêu cầu tiếp theo từ tài khoản này bị chặn đứng hoàn toàn với mã lỗi **`HTTP 403 Forbidden`** (*"Tài khoản của bạn đã bị khóa bởi Quản trị viên"*). Phiên làm việc bị vô hiệu hóa lập tức mà không cần đợi JWT Token hết hạn.

---

### 🔹 Kịch bản 5: Phòng thủ gian lận giá & thao túng dữ liệu đơn hàng (Data Tampering)
- **Tình huống:** Kẻ tấn công can thiệp vào gói tin đặt hàng `POST /api/orders`, cố tình truyền trường `TotalAmount = 0` hoặc sửa đổi giá bán của sản phẩm để mua hàng miễn phí.
- **Thao tác thực hiện:**
  1. Gửi body JSON đặt hàng với giá cố tình chỉnh sửa thành 0 đồng.
- **Kết quả phòng thủ:**
  - `OrdersController.cs` không sử dụng giá do client gửi lên. Máy chủ truy vấn giá gốc niêm yết trong bảng `Products` từ cơ sở dữ liệu và tính toán lại `TotalAmount = UnitPrice * Quantity`.
  - Giá trị đơn hàng được bảo vệ nguyên vẹn theo đúng giá niêm yết của cửa hàng.
