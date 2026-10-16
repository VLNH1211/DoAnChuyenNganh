# WebApi.V2_Secure — Phiên Bản Phòng Thủ Chuyên Sâu (Blue Team)

> **Dự án thành phần thuộc Đồ án Chuyên ngành An toàn thông tin (HUTECH)**  
> **Sinh viên:** Võ Lê Nhật Hoàng | **MSSV:** 2387700017 | **Lớp:** 23DATA1  
> **Nền tảng:** C# ASP.NET Core 8.0 LTS Web API | **Cổng dịch vụ:** `http://localhost:5002`  
> **Cơ sở dữ liệu:** SQL Server LocalDB (`SecurityLab_V2`)

---

## 1. MỤC ĐÍCH & Ý NGHĨA KHOA HỌC

Phiên bản **`WebApi.V2_Secure`** đóng vai trò là **mô hình phòng thủ mục tiêu (Hardened / Blue Team)** trong đề tài nghiên cứu. Toàn bộ các cơ chế kiểm soát bảo mật trong dự án này được thiết kế và triển khai **hoàn toàn trên nền tảng ASP.NET Core 8.0 LTS**, giải quyết triệt để 8 bài toán tấn công thực tế từ mức xác thực, phân quyền, kiểm soát đối tượng, kiểm tra tính toàn vẹn dữ liệu cho đến quản lý vòng đời phiên làm việc và phòng chống từ chối dịch vụ.

Dự án chứng minh năng lực phòng thủ đa lớp (**Defense-in-Depth**) có thể đạt được hoàn toàn thông qua việc vận dụng đúng đắn và chuẩn hóa các thành phần gốc của ASP.NET Core:
1. **Pipeline Middleware** xử lý HTTP Request/Response.
2. **Authentication Handler** chuẩn hóa JWT Bearer (RFC 7519).
3. **Authorization Engine** với Policy-Based và Role-Based Access Control (RBAC).
4. **Entity Framework Core 8.0** với Parameterized Queries và bảo vệ toàn vẹn logic nghiệp vụ.
5. **Dependency Injection & In-Memory Services** kiểm soát Blacklist và Rate Limiting.

---

## 2. GIAO DIỆN CYBER DASHBOARD & BLUE TEAM DEFENSE SIMULATOR (V2)

Ứng dụng tích hợp sẵn giao diện web Cyber Security Dashboard tại `http://localhost:5002` với tông màu **Xanh Lục Bảo Mật (Cyber Green)**:

![Giao diện Bảng điều khiển Bản V2](../screenshots/02_V2_Dashboard_Overview.png)
*Hình 1: Giao diện tổng quan Web V2 tại cổng 5002 với tông xanh an toàn, tích hợp bộ giải mã Claims JWT, 2 hàng kịch bản phòng thủ và hệ thống Security Audit Logs.*

---

## 3. CÁC THÀNH PHẦN BẢO MẬT GỐC CỦA NỀN TẢNG ASP.NET CORE ĐƯỢC ỨNG DỤNG

Dự án áp dụng 100% các công nghệ và cơ chế gốc của nền tảng ASP.NET Core 8.0:

```
                            [ HTTP Request từ Client ]
                                        │
                                        ▼
               ┌──────────────────────────────────────────────────┐
               │    1. ASP.NET Core Kestrel Web Server            │
               └────────────────────────┬─────────────────────────┘
                                        │
                                        ▼
               ┌──────────────────────────────────────────────────┐
               │    2. Custom SecurityAuditMiddleware             │
               │    - Tự động gắn 5 OWASP HTTP Security Headers   │
               │    - Kiểm tra Token Revocation (JTI Blacklist)   │
               └────────────────────────┬─────────────────────────┘
                                        │
                                        ▼
               ┌──────────────────────────────────────────────────┐
               │    3. ASP.NET Core Authentication Middleware     │
               │    - AddAuthentication(JwtBearerDefaults)        │
               │    - TokenValidationParameters (Issuer, Signing) │
               │    - OnChallenge: Trả JSON 401 & ghi AuditLog    │
               └────────────────────────┬─────────────────────────┘
                                        │
                                        ▼
               ┌──────────────────────────────────────────────────┐
               │    4. ASP.NET Core Authorization Middleware      │
               │    - AddAuthorization(Policy "AdminOnly")        │
               │    - OnForbidden: Trả JSON 403 & ghi AuditLog    │
               └────────────────────────┬─────────────────────────┘
                                        │
                                        ▼
               ┌──────────────────────────────────────────────────┐
               │    5. ASP.NET Core Controllers & Action Filters  │
               │    - [Authorize] & [Authorize(Roles = "...")]    │
               │    - Object-Level Check (User.Claims.UserId)     │
               │    - Input Regex Validation & DTO Model Binding  │
               │    - RateLimitingService (Sliding Window 429)    │
               └────────────────────────┬─────────────────────────┘
                                        │
                                        ▼
               ┌──────────────────────────────────────────────────┐
               │    6. Entity Framework Core 8.0 (ORM)            │
               │    - LINQ Parameterized Queries (Chống SQLi)     │
               │    - Phân tách phạm vi dữ liệu (Data Isolation)  │
               │    - Ghi nhận sự kiện vào CSDL SecurityLab_V2    │
               └──────────────────────────────────────────────────┘
```

---

## 4. CHI TIẾT 8 CƠ CHẾ PHÒNG THỦ THỰC NGHIỆM ĐỐI CHỨNG (KB1 – KB8)

### 4.1 Bốn kịch bản bảo mật cốt lõi (Khớp 100% Đề cương đã nộp)

| Kịch bản | Tên kịch bản & Endpoint | Cơ chế phòng thủ ASP.NET Core | Mã chặn | Minh chứng hình ảnh |
| :---: | :--- | :--- | :---: | :---: |
| **KB1** | **Kiểm soát Xác thực**<br>`GET /api/orders` | Khai báo `[Authorize]` trên `OrdersController`. Cấu hình `JwtBearerEvents.OnChallenge` chặn đứng nặc danh, trả về mã `401 Unauthorized` chuẩn và ghi Audit Log. | **401** | ![KB1 After](../screenshots/04_KB1_After_V2_Unauthorized_401.png) |
| **KB1 (Hợp lệ)** | **Xác thực JWT thành công**<br>`POST /api/auth/login` | Cấp JWT Token có `JTI`, giải mã Claims (`sub`, `role`, `userId`). Phân tách phạm vi dữ liệu (Data Isolation): `userA` chỉ thấy đúng 3 đơn của mình. | **200** | ![KB1 UserA](../screenshots/05_KB1_After_V2_ValidToken_UserA_200.png) |
| **KB2** | **Phân quyền vai trò (RBAC)**<br>`GET /api/admin/users` | Khai báo `[Authorize(Policy = "AdminOnly")]`. Khi `userA` (vai trò `NguoiDung`) truy cập, ASP.NET Core Authorization Engine chặn với `403 Forbidden`. | **403** | ![KB2 Blocked](../screenshots/07_KB2_After_V2_RBAC_Blocked_403.png) |
| **KB2 (Admin)** | **Quản trị viên truy cập**<br>`GET /api/admin/users` | Admin truy cập hợp lệ (`200 OK`). Mật khẩu trong CSDL được băm bằng thuật toán **BCrypt** an toàn (`BCrypt.Net-Next`) và ẩn hoàn toàn khỏi phản hồi API. | **200** | ![KB2 Admin](../screenshots/08_KB2_After_V2_Admin_Access_200.png) |
| **KB3** | **Chống BOLA / IDOR**<br>`GET /api/orders/{id}` | Trích xuất `UserId` từ `User.Claims` trong ASP.NET Core Action, đối chiếu với `order.UserId`. Nếu `userA` xem đơn `#3` của `userB`, lập tức chặn `403 Forbidden`. | **403** | ![KB3 Blocked](../screenshots/10_KB3_After_V2_IDOR_Blocked_403.png) |
| **KB4** | **Chống SQL Injection**<br>`GET /api/orders/search` | Kết hợp **Regex Input Validation** lọc mẫu tấn công độc hại (`' OR 1=1 --`) chặn với mã `400 Bad Request` + chuyển đổi 100% sang **EF Core Parameterized LINQ**. | **400** | ![KB4 Blocked](../screenshots/12_KB4_After_V2_SQLi_Blocked_400.png) |

---

### 4.2 Bốn kịch bản phòng thủ chiều sâu mở rộng (Defense-in-Depth)

| Kịch bản | Tên kịch bản & Endpoint | Cơ chế phòng thủ ASP.NET Core | Mã chặn |
| :---: | :--- | :--- | :---: |
| **KB5** | **Thu hồi Token khi Đăng xuất (Token Revocation)**<br>`POST /api/auth/logout` -> `GET /api/auth/me` | Khi Logout, `TokenBlacklistService` (In-Memory Singleton) lưu `JTI` của token kèm thời hạn tự hủy. `SecurityAuditMiddleware` kiểm tra `JTI` và từ chối mọi token bị thu hồi. | **401**<br>*(TokenRevoked)* |
| **KB6** | **Chống Lỗi Nghiệp Vụ, Mass Assignment & IDOR Xóa**<br>`POST /api/orders` \| `DELETE /api/orders/{id}` | - Bắt buộc `TotalAmount > 0` (chặn số tiền âm `-999,999đ` với mã `400`).<br>- Tự động gán `newOrder.UserId = Claims.UserId` (chống client tự gán `UserId` của người khác).<br>- Kiểm tra quyền sở hữu đơn hàng khi Xóa (chặn `403` nếu xóa đơn người khác). | **400 / 403** |
| **KB7** | **Bảo vệ Upload tệp & Nhận diện Magic Bytes**<br>`POST /api/orders/{id}/upload` | - Chống Path Traversal (`..`, `/`, `\`) và Double Extension (`.aspx`, `.php`, `.exe`).<br>- Đọc byte đầu luồng stream (`file.OpenReadStream()`): kiểm tra chữ ký byte thực tế (**JPEG: `FF D8 FF`**, **PNG: `89 50 4E 47`**, **PDF: `%PDF`**). Từ chối tệp ngụy trang với mã `400/415`. | **400 / 415** |
| **KB8** | **Chống Brute-Force & Rate Limiting Spam API**<br>`POST /api/auth/login` \| `GET /api/orders/rate-test` | Triển khai `RateLimitingService` theo thuật toán Sliding Window:<br>- Khóa tạm thời tài khoản/IP nếu đăng nhập sai quá 5 lần.<br>- Giới hạn tối đa 5 yêu cầu / 5 giây trên API. Vượt ngưỡng trả về **`429 Too Many Requests`** kèm Header `Retry-After: 5`. | **429** |

---

### 4.3 Minh chứng kiểm tra liên hoàn toàn bộ cơ chế phòng thủ trên Bản V2

![Run All Kịch bản cốt lõi V2](../screenshots/13b_V2_RunAll.png)
*Hình 2: Chạy liên hoàn 4 kịch bản cốt lõi KB1–KB4 trên Bản V2 — Toàn bộ 4 kịch bản chuyển màu XANH AN TOÀN (401, 403, 403, 400).*

![Run All Kịch bản mở rộng V2](../screenshots/15b_V2_RunAll_Extended_KB5_KB8.png)
*Hình 3: Chạy liên hoàn 4 kịch bản mở rộng KB5–KB8 trên Bản V2 — Toàn bộ 4 cuộc tấn công đều bị chặn đứng an toàn.*

---

## 5. HTTP SECURITY HEADERS CHUẨN OWASP & NHẬT KÝ KIỂM TOÁN (AUDIT LOGS)

### 5.1 Cấu hình 5 OWASP HTTP Security Headers
Được gắn tự động vào mọi phản hồi HTTP thông qua `SecurityAuditMiddleware`:
1. `X-Content-Type-Options: nosniff` (Chống MIME confusion / sniffing)
2. `X-Frame-Options: DENY` (Chống Clickjacking)
3. `X-XSS-Protection: 1; mode=block` (Bộ lọc XSS trình duyệt)
4. `Referrer-Policy: strict-origin-when-cross-origin` (Bảo vệ địa chỉ nguồn)
5. `Content-Security-Policy: default-src 'self' 'unsafe-inline' data:;` (Chặn nạp mã độc từ bên thứ ba)

![OWASP Security Headers Live Check](../screenshots/16_V2_OWASP_Security_Headers_LiveCheck.png)
*Hình 4: Kiểm tra trực tiếp bộ 5 OWASP HTTP Security Headers trên Bản V2 đạt chuẩn OWASP ZAP.*

### 5.2 Hệ thống Nhật ký Kiểm toán An ninh (Security Audit Logs)
`SecurityAuditMiddleware` tự động chặn và ghi nhận toàn bộ các sự kiện vi phạm an ninh (`401`, `403`, `400`, `415`, `429`) vào bảng CSDL `AuditLogs`:

![Security Audit Logs Table](../screenshots/14_V2_Security_Audit_Logs_Table.png)
*Hình 5: Bảng nhật ký kiểm toán an ninh trên Bản V2 hiển thị đầy đủ vết tấn công phục vụ điều tra số (Digital Forensics).*

---

## 6. HƯỚNG DẪN KHỞI ĐỘNG & KIỂM THỬ BẢN V2

### Khởi động độc lập
Tại thư mục gốc `ĐACN`, mở terminal PowerShell và chạy:
```powershell
dotnet run --project WebApi.V2_Secure/WebApi.V2_Secure.csproj --urls=http://localhost:5002
```

### Chạy kiểm thử tự động 17 tiêu chí bằng script PowerShell
```powershell
powershell -ExecutionPolicy Bypass -File .\test_api.ps1
```
*Kết quả xác nhận PASS 17/17 tiêu chí kiểm thử an toàn.*

### Các đường dẫn kiểm thử
- **Giao diện Dashboard V2:** `http://localhost:5002`
- **Swagger UI (kèm Authorize Bearer JWT):** `http://localhost:5002/swagger`
