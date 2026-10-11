# HỆ THỐNG THỰC NGHIỆM ĐỒ ÁN CHUYÊN NGÀNH AN TOÀN THÔNG TIN

## ĐỀ TÀI: XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO ỨNG DỤNG WEB/API DỰA TRÊN XÁC THỰC, PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP

- **Sinh viên thực hiện:** **Võ Lê Nhật Hoàng**
- **Mã số sinh viên (MSSV):** **2387700017**
- **Lớp:** **23DATA1**
- **Chuyên ngành:** **An toàn thông tin (ATTT)**
- **Đơn vị:** Viện Công nghệ Thông tin - Trường Đại học Công nghệ TP.HCM (**HUTECH**)

---

## 1. CẤU TRÚC SOLUTION & THƯ MỤC

Hệ thống được tổ chức hoàn chỉnh theo cấu trúc chuẩn dự án doanh nghiệp và nghiên cứu khoa học (mô hình đối chứng **Before/After: Red Team vs Blue Team**):

```
ĐACN/
│
├── SecurityLab.sln                     # File Solution C# mở bằng Visual Studio / VS Code
├── SecurityLab.slnx                    # File Solution định dạng mới
├── run_all.ps1 / run_all.bat           # Script khởi chạy đồng thời cả 2 dự án (5001 & 5002)
├── test_api.ps1 / test_api.bat         # Script PowerShell kiểm thử tự động 17 tiêu chí (KB1–KB8 + Headers + Audit)
├── capture_all_screenshots.py          # Script Playwright tự động chạy Demo & chụp 18 ảnh Full HD
├── export_to_word.py                   # Script xuất báo cáo Word BO_MINH_CHUNG_HINH_ANH_BEFORE_AFTER.docx
├── README.md                           # Hướng dẫn tổng quan hệ thống
│
├── WebApi.V1_Vulnerable/               # [PHIÊN BẢN V1 - BASELINE] (Cổng: http://localhost:5001)
│   ├── wwwroot/                        # Giao diện Web Cyber Dashboard & Simulator V1 (Theme Cảnh báo Đỏ/Cam)
│   ├── Controllers/                    # AuthController, OrdersController, AdminController (Cố ý chứa lỗ hổng KB1–KB8)
│   ├── Data/                           # V1DbContext, DbInitializer (Tự động nạp CSDL SecurityLab_V1)
│   ├── Models/                         # User (Plaintext password), Role, Order, AuditLog
│   ├── DTOs/                           # AuthDtos, OrderDtos (Cho phép Mass Assignment)
│   └── Properties/launchSettings.json  # Cấu hình cổng 5001
│
├── WebApi.V2_Secure/                   # [PHIÊN BẢN V2 - SECURE] (Cổng: http://localhost:5002)
│   ├── wwwroot/                        # Giao diện Web Cyber Dashboard & Simulator V2 (Theme Bảo mật Xanh Ngọc)
│   ├── Controllers/                    # AuthController, OrdersController, AdminController (Đã gia cố KB1–KB8)
│   ├── Data/                           # V2DbContext, DbInitializer (Tự động nạp CSDL SecurityLab_V2)
│   ├── Models/                         # User (Băm BCrypt), Role, Order, AuditLog
│   ├── Middleware/                     # SecurityAuditMiddleware (Gắn OWASP Headers, kiểm tra JTI Blacklist, ghi Audit Log)
│   ├── Services/                       # AuditLogService, TokenBlacklistService (JTI Revocation), RateLimitingService (429)
│   ├── DTOs/                           # AuthDtos, CommonDtos, OrderDtos (Chống Mass Assignment)
│   └── Properties/launchSettings.json  # Cấu hình cổng 5002, Swagger có nút "Authorize" Bearer JWT
│
├── sql/
│   └── init_and_verify.sql             # Toàn bộ mã T-SQL tạo bảng, nạp seed data & truy vấn kiểm chứng SSMS
│
├── postman/
│   └── Postman_Collection_KB1_KB4.json # Bộ kịch bản Postman Collection kiểm thử tự động (Assertions PASS 100%)
│
├── screenshots/                        # Thư mục lưu 18 ảnh chụp thực nghiệm Full HD (1920x1080)
│
└── docs/
    ├── MA_TRAN_PHAN_QUYEN_VA_BAO_CAO_BEFORE_AFTER.md # Ma trận phân quyền, Báo cáo Before/After KB1–KB8 & Hướng dẫn ZAP
    └── BO_MINH_CHUNG_HINH_ANH_BEFORE_AFTER.docx      # Báo cáo Word đính kèm đầy đủ 18 ảnh minh chứng Before/After
```

---

## 2. DỮ LIỆU MẪU HỆ THỐNG (SEED DATA)

CSDL được tự động khởi tạo khi chạy ứng dụng (`MSSQLLocalDB`). Bạn cũng có thể mở SSMS chạy trực tiếp file [init_and_verify.sql](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/%C4%90ACN/sql/init_and_verify.sql).

| Tài khoản (Username) | Mật khẩu | Vai trò (Role) | Họ và tên | Quyền sở hữu đơn hàng |
| :--- | :--- | :--- | :--- | :--- |
| `admin` | `Admin@123` | `Admin` | Hệ thống Quản Trị Viên | Toàn quyền xem người dùng, đơn hàng và Audit Log |
| `staff1` | `Staff@123` | `NhanVien` | Nguyễn Văn Staff | Xem/duyệt toàn bộ đơn hàng, bị cấm truy cập API Admin |
| `userA` | `UserA@123` | `NguoiDung` | Võ Lê Nhật Hoàng | Chủ sở hữu các đơn hàng: **#1, #2, #5** |
| `userB` | `UserB@123` | `NguoiDung` | Trần Thị Bích | Chủ sở hữu các đơn hàng: **#3, #4** |

---

## 3. CÁCH KHỞI ĐỘNG HỆ THỐNG

### Cách 1: Sử dụng Script tự động `run_all.ps1` hoặc `run_all.bat` (Khuyên dùng)
Tại thư mục `ĐACN`, mở PowerShell và chạy:
```powershell
.\run_all.ps1
```
Hệ thống sẽ mở 2 cửa sổ terminal riêng biệt để chạy song song:
- **Bản V1 (Red Team - Có lỗ hổng):** `http://localhost:5001` (Swagger: `http://localhost:5001/swagger`)
- **Bản V2 (Blue Team - Đã bảo mật):** `http://localhost:5002` (Swagger: `http://localhost:5002/swagger`)

### Cách 2: Chạy thủ công từng dự án
- **Terminal 1:**
  ```powershell
  dotnet run --project WebApi.V1_Vulnerable/WebApi.V1_Vulnerable.csproj --urls=http://localhost:5001
  ```
- **Terminal 2:**
  ```powershell
  dotnet run --project WebApi.V2_Secure/WebApi.V2_Secure.csproj --urls=http://localhost:5002
  ```

---

## 4. TỔNG HỢP 8 KỊCH BẢN BẢO MẬT ĐỐI CHỨNG (KB1–KB4 CỐT LÕI & KB5–KB8 MỞ RỘNG)

### 4.1 Bốn kịch bản bảo mật cốt lõi (Khớp 100% Đề cương đã nộp)

| Kịch bản | Tên kịch bản & Endpoint | Bản V1 (Before - Lỗ hổng) | Bản V2 (After - Đã bảo vệ) | Mã chặn |
| :---: | :--- | :--- | :--- | :---: |
| **KB1** | Truy cập API khi chưa đăng nhập<br>`GET /api/orders` | Trả về `200 OK` không cần token -> Rò rỉ toàn bộ đơn hàng nội bộ. | Yêu cầu JWT Bearer Token hợp lệ -> Thiếu token bị chặn ngay lập tức. | **401 Unauthorized** |
| **KB2** | Người dùng thường gọi API quản trị<br>`GET /api/admin/users` | Trả về `200 OK` cho bất kỳ ai -> Lộ toàn bộ tài khoản và mật khẩu thô. | Áp dụng RBAC Policy `AdminOnly` + Băm mật khẩu BCrypt. | **403 Forbidden** |
| **KB3** | Khai thác BOLA / IDOR xem đơn người khác<br>`GET /api/orders/{id}` | `userA` đổi `id=3` (đơn của `userB`) vẫn xem được dữ liệu bình thường. | Kiểm tra chủ sở hữu (`order.UserId == Claims.UserId`) -> Chặn và ghi Audit Log. | **403 Forbidden** |
| **KB4** | Đầu vào không hợp lệ / SQL Injection<br>`GET /api/orders/search?keyword=...` | Ghép chuỗi SQL thô -> Bị khai thác `' OR 1=1 --` lộ sạch CSDL hoặc lỗi `500`. | Input Validation (Regex + độ dài) + Parameterized EF Core LINQ. | **400 Bad Request** |

### 4.2 Bốn kịch bản phòng thủ chiều sâu mở rộng (Defense-in-Depth)

| Kịch bản | Tên kịch bản & Endpoint | Bản V1 (Before - Lỗ hổng) | Bản V2 (After - Đã bảo vệ) | Mã chặn |
| :---: | :--- | :--- | :--- | :---: |
| **KB5** | Quản lý vòng đời JWT & Thu hồi Token khi Logout<br>`POST /api/auth/logout` -> `GET /api/auth/me` | Logout chỉ xóa ở client; kẻ tấn công phát lại (Replay) JWT cũ vẫn gọi được API (`200 OK`). | Lưu `JTI` vào `TokenBlacklistService` khi Logout; Middleware chặn mọi JWT đã thu hồi. | **401 Unauthorized**<br>*(TokenRevoked)* |
| **KB6** | Lỗi Logic nghiệp vụ, Mass Assignment & IDOR Sửa/Xóa<br>`POST /api/orders` \| `PUT/DELETE /api/orders/{id}` | Cho phép tạo đơn số tiền âm (`-999,999đ`), giả mạo `UserId`/`Status="Completed"`, xóa đơn của `userB`. | Tự động lấy `UserId` từ JWT, bắt buộc `TotalAmount > 0`, kiểm tra chủ sở hữu khi Sửa/Xóa. | **400 Bad Request** /<br>**403 Forbidden** |
| **KB7** | Kiểm tra Chữ ký Byte (Magic Bytes) khi Upload tệp<br>`POST /api/orders/{id}/upload` | Chấp nhận mọi file theo tên, cho phép tải lên `webshell.jpg.aspx` hoặc script giả mạo ảnh. | Chặn Path Traversal, chặn Double Extension, kiểm tra **Magic Bytes** đầu file (`FF D8 FF`, `89 50 4E 47`, `%PDF`). | **400 / 415**<br>*(UnsupportedMediaType)* |
| **KB8** | Chống Brute-Force Login & Rate Limiting Spam API<br>`POST /api/auth/login` \| `GET /api/orders/rate-test` | Không giới hạn tần suất request -> Dễ bị tấn công từ điển (Brute-force) và làm nghẽn API (DoS). | `RateLimitingService` khóa tạm thời khi sai mật khẩu > 5 lần hoặc vượt ngưỡng 5 req/5s kèm `Retry-After`. | **429 Too Many Requests** |

---

## 5. HƯỚNG DẪN KIỂM THỬ

### 5.1 Kiểm thử tự động 17 tiêu chí bằng PowerShell
Khi 2 bản API đang chạy, mở PowerShell và thực thi:
```powershell
.\test_api.ps1
```
Script sẽ tự động chạy **17 bước kiểm thử đối chứng** (bao gồm `KB1–KB8`, kiểm tra `OWASP HTTP Security Headers` và kiểm tra bảng `AuditLogs`) và xác nhận **PASS 17/17**.

### 5.2 Kiểm thử bằng Postman
1. Mở Postman -> **Import** -> chọn [Postman_Collection_KB1_KB4.json](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/%C4%90ACN/postman/Postman_Collection_KB1_KB4.json).
2. Chạy lần lượt các thư mục từ `00` đến `05`. Tất cả các Assertions đều tự động kiểm tra mã phản hồi (`200`, `401`, `403`, `400`), token và cấu trúc dữ liệu trả về.

### 5.3 Đối chiếu trực tiếp trên SQL Server (SSMS)
Mở SSMS, kết nối tới `(localdb)\MSSQLLocalDB` và mở file [init_and_verify.sql](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/%C4%90ACN/sql/init_and_verify.sql) để thực thi các câu truy vấn kiểm chứng.

### 5.4 Quét kiểm thử bảo mật bằng OWASP ZAP
Xem hướng dẫn chi tiết từng bước trong tài liệu [MA_TRAN_PHAN_QUYEN_VA_BAO_CAO_BEFORE_AFTER.md](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/%C4%90ACN/docs/MA_TRAN_PHAN_QUYEN_VA_BAO_CAO_BEFORE_AFTER.md).
