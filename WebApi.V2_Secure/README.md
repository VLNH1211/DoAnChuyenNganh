# 🟢 WEB V2 (`WebApi.V2_Secure`) — PHIÊN BẢN ĐÃ BẢO MẬT ĐA LỚP (BLUE TEAM HARDENED)

- **Địa chỉ chạy:** `http://localhost:5002` (Swagger UI kèm nút Authorize JWT: `http://localhost:5002/swagger`)
- **Cơ sở dữ liệu:** `SecurityLab_V2` (SQL Server LocalDB)
- **Sinh viên thực hiện:** **Võ Lê Nhật Hoàng** (MSSV: `2387700017` - Lớp: `23DATA1` - Ngành An toàn thông tin HUTECH)

---

## 1. MỤC ĐÍCH CỦA WEB V2 TRONG ĐỒ ÁN

**Web V2 (`WebApi.V2_Secure`)** là phiên bản **After (Hardened Security)**, đóng vai trò hệ thống **Phòng thủ đa lớp (Blue Team Defense-in-Depth)**.

Khi bảo vệ đồ án trước Hội đồng, sinh viên mở **Web V2 (`http://localhost:5002`)** song song với Web V1 để chứng minh toàn bộ 8 kịch bản tấn công (`KB1–KB4` cốt lõi theo đề cương + `KB5–KB8` mở rộng) đều bị chặn đứng chính xác với các mã trạng thái chuẩn HTTP (`401`, `403`, `400`, `415`, `429`) và được ghi vết 100% vào bảng `AuditLogs`.

---

## 2. HÌNH ẢNH GIAO DIỆN & MINH CHỨNG PHÒNG THỦ TRÊN WEB V2

### 🟢 Hình 1: Giao diện Tổng quan Bảng điều khiển Web V2 (`http://localhost:5002`)
![Tổng quan Web V2](../screenshots/02_V2_Dashboard_Overview.png)
> Giao diện mang tông màu **Xanh Ngọc An Toàn**, hiển thị rõ 8 huy hiệu bảo vệ (`KB1–KB8`), bộ giải mã JWT Claims trực tiếp (`sub`, `role`, `jti`), 4 Tab nghiệp vụ và bảng giám sát **Security Audit Logs**.

---

### 🟢 Hình 2: Phòng thủ KB1 — Chặn truy cập nặc danh với mã `401 Unauthorized` (`GET /api/orders`)
![Phòng thủ KB1 401 trên V2](../screenshots/04_KB1_After_V2_Unauthorized_401.png)
> **Cơ chế bảo vệ:** Áp dụng `[Authorize]` kết hợp xác thực chữ ký số **JWT Bearer (HMAC-SHA256)**. Mọi request thiếu Token hoặc Token hết hạn đều bị chặn ngay tại Middleware với mã `401 Unauthorized`.

---

### 🟢 Hình 3: Phòng thủ KB1 (Hợp lệ) — Xác thực JWT `userA` & Phân tách phạm vi dữ liệu (`200 OK`)
![Phòng thủ KB1 200 trên V2](../screenshots/05_KB1_After_V2_ValidToken_UserA_200.png)
> **Cơ chế bảo vệ:** Khi `userA` đăng nhập hợp lệ, API trích xuất `userId` từ JWT Claims và lọc `WHERE UserId == userId`, chỉ trả về đúng các đơn hàng thuộc sở hữu của `userA` (**Data Isolation**).

---

### 🟢 Hình 4: Phòng thủ KB2 — Chặn người dùng thường gọi API Admin với mã `403 Forbidden` (`GET /api/admin/users`)
![Phòng thủ KB2 403 trên V2](../screenshots/07_KB2_After_V2_RBAC_Blocked_403.png)
> **Cơ chế bảo vệ:** Áp dụng `[Authorize(Policy = "AdminOnly")]` (`RequireRole("Admin")`). Khi `userA` (Role `NguoiDung`) hoặc `staff1` (Role `NhanVien`) gọi API quản trị, hệ thống từ chối ngay với mã `403 Forbidden`.

---

### 🟢 Hình 5: Phòng thủ KB2 (Admin hợp lệ) — Quản trị viên truy cập & Mật khẩu băm BCrypt an toàn (`200 OK`)
![Phòng thủ KB2 Admin 200 trên V2](../screenshots/08_KB2_After_V2_Admin_Access_200.png)
> **Cơ chế bảo vệ:** Chỉ tài khoản `admin` mới nhận được `200 OK`, đồng thời mật khẩu trong CSDL được băm bằng thuật toán **BCrypt** và được che ẩn trên DTO phản hồi.

---

### 🟢 Hình 6: Phòng thủ KB3 — Chặn khai thác BOLA / IDOR với mã `403 Forbidden` (`GET /api/orders/3`)
![Phòng thủ KB3 403 trên V2](../screenshots/10_KB3_After_V2_IDOR_Blocked_403.png)
> **Cơ chế bảo vệ:** Kiểm soát truy cập mức đối tượng (**Object-Level Access Control**): kiểm tra `if (order.UserId != currentUserId && role != "Admin") return StatusCode(403)`. `userA` không thể xem trộm đơn hàng `#3` của `userB`.

---

### 🟢 Hình 7: Phòng thủ KB4 — Chặn tấn công SQL Injection với mã `400 Bad Request` (`GET /api/orders/search`)
![Phòng thủ KB4 400 trên V2](../screenshots/12_KB4_After_V2_SQLi_Blocked_400.png)
> **Cơ chế bảo vệ:** Kết hợp 2 lớp: (1) Kiểm tra Regex Whitelist & chặn các từ khóa SQL nguy hiểm (`'`, `--`, `OR 1=1`, `DROP`, `UNION`...) trả về `400 Bad Request`; (2) Truy vấn hoàn toàn qua **Parameterized EF Core LINQ**, không bao giờ gây lỗi `500`.

---

### 🟢 Hình 8: Chạy liên hoàn 4 kịch bản cốt lõi (`Run All KB1–KB4`) trên Web V2
![Run All KB1-4 trên V2](../screenshots/13b_V2_RunAll.png)
> Toàn bộ 4 kịch bản cốt lõi theo đề cương đều chuyển sang màu **XANH AN TOÀN (`401`, `403`, `403`, `400`)**.

---

### 🟢 Hình 9: Chạy liên hoàn 4 kịch bản phòng thủ chiều sâu mở rộng (`Run All KB5–KB8`) trên Web V2
![Run All KB5-8 trên V2](../screenshots/15b_V2_RunAll_Extended_KB5_KB8.png)
> Toàn bộ 4 cơ chế mở rộng trên Web V2 đều chặn đứng cuộc tấn công:
> - **KB5 (`TokenBlacklistService.cs`):** Thu hồi mã `JTI` khi Logout, chặn Token Replay với mã **`401 Unauthorized (TokenRevoked)`**.
> - **KB6 (`OrdersController.cs` & `OrderDtos.cs`):** Tự động lấy `UserId` từ JWT (chống Mass Assignment), chặn số tiền âm với mã **`400 Bad Request`**, chặn xóa đơn người khác với mã **`403 Forbidden`**.
> - **KB7 (`UploadAttachment`):** Kiểm tra chữ ký byte thực tế (**Magic Bytes**: `FF D8 FF`, `89 50 4E 47`, `%PDF`) và chặn đuôi mở rộng kép `webshell.jpg.aspx` với mã **`400 / 415`**.
> - **KB8 (`RateLimitingService.cs`):** Thuật toán Sliding Window chặn hành vi spam API vượt ngưỡng 5 req/5s với mã **`429 Too Many Requests`** kèm Header `Retry-After: 5`.

---

### 🟢 Hình 10: Kiểm tra trực tiếp 5 OWASP HTTP Security Headers trên Web V2
![OWASP Security Headers trên V2](../screenshots/16_V2_OWASP_Security_Headers_LiveCheck.png)
> `SecurityAuditMiddleware` tự động gắn đầy đủ 5 Header an ninh chuẩn OWASP (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `X-XSS-Protection: 1; mode=block`, `Referrer-Policy`, `Content-Security-Policy`).

---

### 🟢 Hình 11: Hệ thống Nhật ký Kiểm toán An ninh (`AuditLogs`) trên Web V2
![Audit Logs trên V2](../screenshots/14_V2_Security_Audit_Logs_Table.png)
> Lưu vết đầy đủ mọi hành vi tấn công bị chặn (`401`, `403`, `400`, `415`, `429`) vào bảng `AuditLogs` phục vụ điều tra số (Digital Forensics).

---

## 3. CÁCH CHẠY RIÊNG WEB V2
Tại thư mục `ĐACN`, chạy lệnh:
```powershell
dotnet run --project WebApi.V2_Secure/WebApi.V2_Secure.csproj --urls=http://localhost:5002
```
