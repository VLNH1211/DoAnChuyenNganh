# HỆ THỐNG THỰC NGHIỆM ĐỒ ÁN CHUYÊN NGÀNH AN TOÀN THÔNG TIN

## ĐỀ TÀI: XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO ỨNG DỤNG WEB/API DỰA TRÊN XÁC THỰC, PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP

- **Sinh viên thực hiện:** **Võ Lê Nhật Hoàng**
- **Mã số sinh viên (MSSV):** **2387700017**
- **Lớp:** **23DATA1**
- **Chuyên ngành:** **An toàn thông tin (ATTT)**
- **Đơn vị:** Viện Công nghệ Thông tin - Trường Đại học Công nghệ TP.HCM (**HUTECH**)

---

## 1. CẤU TRÚC SOLUTION & THƯ MỤC

Hệ thống được tổ chức hoàn chỉnh theo cấu trúc chuẩn dự án doanh nghiệp và nghiên cứu khoa học:

```
ĐACN/
│
├── SecurityLab.sln                     # File Solution C# mở bằng Visual Studio / VS Code
├── SecurityLab.slnx                    # File Solution định dạng .NET 10 / mới nhất
├── run_all.ps1                         # Script PowerShell khởi chạy đồng thời cả 2 dự án (5001 & 5002)
├── test_api.ps1                        # Script PowerShell kiểm thử tự động 10 kịch bản trên V2
├── README.md                           # Hướng dẫn tổng quan hệ thống
│
├── WebApi.V1_Vulnerable/               # [PHIÊN BẢN V1 - BASELINE] (Cổng: http://localhost:5001)
│   ├── wwwroot/                        # Giao diện Web Cyber Dashboard & Simulator V1 (Theme Cảnh báo Đỏ/Cam)
│   ├── Controllers/                    # AuthController, OrdersController, AdminController
│   ├── Data/                           # V1DbContext, DbInitializer (Tự động nạp CSDL SecurityLab_V1)
│   ├── Models/                         # User, Role, Order, AuditLog
│   ├── DTOs/                           # Data Transfer Objects
│   └── Properties/launchSettings.json  # Cấu hình cổng 5001
│
├── WebApi.V2_Secure/                   # [PHIÊN BẢN V2 - SECURE] (Cổng: http://localhost:5002)
│   ├── wwwroot/                        # Giao diện Web Cyber Dashboard & Simulator V2 (Theme Bảo mật Xanh Ngọc)
│   ├── Controllers/                    # AuthController, OrdersController, AdminController
│   ├── Data/                           # V2DbContext, DbInitializer (Tự động nạp CSDL SecurityLab_V2)
│   ├── Models/                         # User (Băm BCrypt), Role, Order, AuditLog
│   ├── Middleware/                     # SecurityAuditMiddleware (Chặn và ghi log 401, 403, 400)
│   ├── Services/                       # IAuditLogService, AuditLogService
│   ├── DTOs/                           # LoginRequest, LoginResponse, ApiResponse, UserDto
│   └── Properties/launchSettings.json  # Cấu hình cổng 5002, Swagger có nút "Authorize" Bearer JWT
│
├── sql/
│   └── init_and_verify.sql             # Toàn bộ mã T-SQL tạo bảng, nạp seed data & truy vấn kiểm chứng SSMS
│
├── postman/
│   └── Postman_Collection_KB1_KB4.json # Bộ kịch bản Postman Collection kiểm thử tự động KB1-KB4 (Assertions PASS 100%)
│
└── docs/
    └── MA_TRAN_PHAN_QUYEN_VA_BAO_CAO_BEFORE_AFTER.md # Ma trận phân quyền, Báo cáo Before/After & Hướng dẫn quét ZAP
```

---

## 2. DỮ LIỆU MẪU HỆ THỐNG (SEED DATA)

CSDL được tự động khởi tạo khi chạy ứng dụng (`MSSQLLocalDB`). Bạn cũng có thể mở SSMS chạy trực tiếp file [init_and_verify.sql](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/ĐACN/sql/init_and_verify.sql).

| Tài khoản (Username) | Mật khẩu | Vai trò (Role) | Họ và tên | Quyền sở hữu đơn hàng |
| :--- | :--- | :--- | :--- | :--- |
| `admin` | `Admin@123` | `Admin` | Hệ thống Quản Trị Viên | Toàn quyền xem người dùng, đơn hàng và Audit Log |
| `staff1` | `Staff@123` | `NhanVien` | Nguyễn Văn Staff | Xem toàn bộ đơn hàng, bị cấm truy cập API Admin |
| `userA` | `UserA@123` | `NguoiDung` | Võ Lê Nhật Hoàng | Chủ sở hữu các đơn hàng: **#1, #2, #5** |
| `userB` | `UserB@123` | `NguoiDung` | Trần Thị Bích | Chủ sở hữu các đơn hàng: **#3, #4** |

---

## 3. CÁCH KHỞI ĐỘNG HỆ THỐNG

### Cách 1: Sử dụng Script tự động `run_all.ps1` (Khuyên dùng)
Tại thư mục `ĐACN`, mở PowerShell và chạy:
```powershell
.\run_all.ps1
```
Hệ thống sẽ mở 2 cửa sổ terminal riêng biệt để chạy song song:
- **Bản V1:** `http://localhost:5001` (Swagger: `http://localhost:5001/swagger`)
- **Bản V2:** `http://localhost:5002` (Swagger: `http://localhost:5002/swagger`)

### Cách 2: Chạy thủ công từng dự án
- **Terminal 1:**
  ```powershell
  dotnet run --project WebApi.V1_Vulnerable/WebApi.V1_Vulnerable.csproj
  ```
- **Terminal 2:**
  ```powershell
  dotnet run --project WebApi.V2_Secure/WebApi.V2_Secure.csproj
  ```

---

## 4. TÓM TẮT 4 KỊCH BẢN BẢO MẬT (KB1 – KB4)

| Kịch bản | Tên kịch bản & Endpoint | Bản V1 (Before - Lỗ hổng) | Bản V2 (After - Đã bảo vệ) | Mã chặn & Ý nghĩa |
| :---: | :--- | :--- | :--- | :---: |
| **KB1** | Truy cập API khi chưa đăng nhập<br>`GET /api/orders` | Trả về `200 OK` mà không cần token -> Rò rỉ thông tin nội bộ. | Yêu cầu JWT Bearer Token -> Nếu thiếu trả về mã chặn, hợp lệ trả về `200 OK`. | **401 Unauthorized** |
| **KB2** | Người dùng thường gọi API quản trị<br>`GET /api/admin/users` | Trả về `200 OK` cho bất kỳ ai -> Lộ toàn bộ tài khoản và mật khẩu. | Áp dụng RBAC Policy `AdminOnly` -> Chỉ Admin được phép, tài khoản khác bị chặn. | **403 Forbidden** |
| **KB3** | Truy cập dữ liệu tài khoản khác (IDOR)<br>`GET /api/orders/{id}` | `userA` đổi `id=3` (đơn của `userB`) vẫn xem được dữ liệu bình thường. | Kiểm tra quyền sở hữu đối tượng -> `userA` xem đơn của `userB` bị chặn, ghi Audit Log. | **403 Forbidden** |
| **KB4** | Đầu vào không hợp lệ / SQL Injection<br>`GET /api/orders/search?keyword=...` | Ghép chuỗi SQL thô -> Bị khai thác `' OR 1=1 --` lộ sạch CSDL hoặc lỗi `500`. | Input Validation nghiêm ngặt + Parameterized EF Core LINQ -> Chặn đứng SQLi. | **400 Bad Request** |

---

## 5. HƯỚNG DẪN KIỂM THỬ

### 5.1 Kiểm thử tự động bằng PowerShell
Khi 2 bản API đang chạy, mở một PowerShell mới và thực thi:
```powershell
.\test_api.ps1
```
Script sẽ tự động chạy 10 kịch bản kiểm thử đối chứng và in ra kết quả chi tiết từng bước.

### 5.2 Kiểm thử bằng Postman
1. Mở Postman -> **Import** -> chọn [Postman_Collection_KB1_KB4.json](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/ĐACN/postman/Postman_Collection_KB1_KB4.json).
2. Chạy lần lượt các thư mục từ `00` đến `05`. Tất cả các Assertions đều tự động kiểm tra mã phản hồi (200, 401, 403, 400), token và cấu trúc dữ liệu trả về.

### 5.3 Đối chiếu trực tiếp trên SQL Server (SSMS)
Mở SSMS, kết nối tới `(localdb)\MSSQLLocalDB` và mở file [init_and_verify.sql](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/ĐACN/sql/init_and_verify.sql) để thực thi 4 câu truy vấn kiểm chứng.

### 5.4 Quét kiểm thử bảo mật bằng OWASP ZAP
Xem hướng dẫn chi tiết từng bước trong tài liệu [MA_TRAN_PHAN_QUYEN_VA_BAO_CAO_BEFORE_AFTER.md](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/ĐACN/docs/MA_TRAN_PHAN_QUYEN_VA_BAO_CAO_BEFORE_AFTER.md#5-hướng-dẫn-quét-lỗ-hổng-bằng-công-cụ-owasp-zap-zap-scanning).
