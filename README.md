# 🛡️ HỆ THỐNG THỰC NGHIỆM ĐỒ ÁN CHUYÊN NGÀNH AN TOÀN THÔNG TIN

## ĐỀ TÀI: XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO ỨNG DỤNG WEB/API DỰA TRÊN XÁC THỰC, PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP

- **Chuyên ngành:** **An toàn thông tin (ATTT)**
- **Nền tảng công nghệ:** C# ASP.NET Core 8.0 LTS Web API & SQL Server LocalDB
- **Mô hình nghiên cứu:** Thực nghiệm đối chứng chuyên sâu (**Before / After Testing Model - Red Team vs Blue Team**)

---

## 📌 1. GIỚI THIỆU HAI PHIÊN BẢN WEB ĐỐI CHỨNG (BEFORE / AFTER)

Hệ thống được thiết kế và triển khai song song hai phiên bản độc lập để đối chứng rõ nét hiệu quả của từng lớp kiểm soát an toàn thông tin:

| Phiên bản | Địa chỉ dịch vụ | Vai trò trong Đồ án | Giao diện & Đặc điểm kỹ thuật |
| :--- | :--- | :--- | :--- |
| 🔴 **Web V1 (`WebApi.V1_Vulnerable`)** | `http://localhost:5001`<br>[Xem README riêng của V1](./WebApi.V1_Vulnerable/README.md) | **Before (Red Team - Có lỗ hổng)**<br>Mô hình đối chứng cơ sở: Cố ý thiếu hoặc cấu hình sai các cơ chế bảo mật để kiểm thử khai thác trực tiếp 8 lỗ hổng Web/API phổ biến. | Giao diện tông **Đỏ Cảnh Báo**, lưu mật khẩu Plaintext, thiếu kiểm tra JWT, thiếu phân quyền RBAC, dính lỗi BOLA/IDOR Xem/Sửa/Xóa, dính SQL Injection nối chuỗi thô, dính Token Replay sau Logout, chấp nhận đơn tiền âm & Webshell giả mạo ảnh, không áp dụng Rate Limiting. |
| 🟢 **Web V2 (`WebApi.V2_Secure`)** | `http://localhost:5002`<br>[Xem README riêng của V2](./WebApi.V2_Secure/README.md) | **After (Blue Team - Đã bảo mật)**<br>Mô hình phòng thủ kiên cố: Tích hợp đầy đủ 4 lớp cốt lõi và 4 cơ chế bảo vệ chuyên sâu theo chuẩn OWASP API Security. | Giao diện tông **Xanh Lục An Toàn**, băm mật khẩu **BCrypt**, bắt buộc **JWT Bearer (`401`)**, phân quyền **RBAC `AdminOnly` (`403`)**, chống **BOLA/IDOR (`403`)**, lọc **Regex + Parameterized LINQ (`400`)**, thu hồi **JTI Blacklist (`401`)**, kiểm tra **Magic Bytes (`415`)**, chống Spam **Rate Limit (`429`)**, gắn **5 OWASP Security Headers** & ghi **Audit Logs**. |

---

## 🖥️ 2. KIẾN TRÚC GIAO DIỆN 2 TẦNG (TWO-VIEW ARCHITECTURE)

Hệ thống đã được thiết kế lại theo chuẩn ứng dụng Doanh nghiệp thực tế, phân tách rõ ràng giữa **Cổng Xác Thực** và **Bảng Điều Khiển Quản Trị**:

### 🚪 Tầng 1: Cổng Xác Thực Tập Trung (Login Portal)
* **Chức năng:** Giao diện thẻ kính mờ (Glassmorphism) đón tiếp người dùng trước khi vào hệ thống.
* **Hỗ trợ thực nghiệm:**
  - **Quick Login (1-Click):** Đăng nhập nhanh các tài khoản mẫu đại diện cho từng vai trò (`userA`, `userB`, `staff1`, `admin`).
  - **Form Đăng nhập thủ công:** Cho phép nhập `Username` và `Password` tùy ý.
  - **Chế độ Nặc danh (Anonymous Test Mode):** Cho phép kẻ tấn công bỏ qua đăng nhập và truy cập thẳng Dashboard để thực nghiệm lỗ hổng xác thực (KB1).

| Cổng Đăng Nhập Bản V1 (Port 5001) | Cổng Đăng Nhập Bản V2 (Port 5002) |
| :---: | :---: |
| ![V1 Login Portal](./screenshots/00a_V1_Login_Portal.png) | ![V2 Login Portal](./screenshots/00b_V2_Login_Portal.png) |

---

### 📊 Tầng 2: Bảng Điều Khiển Quản Trị & Thực Nghiệm (Dashboard)
* Sau khi đăng nhập thành công (hoặc vào chế độ nặc danh), hệ thống chuyển sang màn hình Dashboard nghiệp vụ hoàn chỉnh.
* **Khu vực A (Thông tin phiên & Token Inspector):** Hiển thị Avatar, vai trò, nút Chuyển nhanh tài khoản (Quick Switch), nút Đăng xuất, chuỗi JWT Token rút gọn và khung giải mã trực tiếp Payload Claims JSON (`sub`, `unique_name`, `role`, `jti`,...).
* **Khu vực B (Nghiệp vụ cốt lõi):** Gồm 4 tab chức năng thực tế:
  1. `📦 Quản Lý Đơn Hàng (/api/orders)`: Tìm kiếm, lọc theo mã/tên, xem đơn theo ID và hiển thị bảng dữ liệu.
  2. `➕ Tạo Đơn Hàng Mới`: Tạo đơn, gửi kèm hóa đơn chứng từ file đính kèm.
  3. `👥 Quản Trị Người Dùng (/api/admin/users)`: Khu vực quản lý tài khoản dành riêng cho Admin.
  4. `🛡️ HTTP Security Headers (OWASP ZAP)`: Kiểm tra trực tiếp các cờ bảo vệ HTTP Header.
* **Khu vực C (Giám sát thực thi):**
  - **Live HTTP Request / Response Inspector:** Theo dõi thời gian thực URL gọi, Request Headers, mã HTTP Status và JSON phản hồi.
  - **Nhật Ký An Ninh & Kiểm Toán (Audit Logs):** Bảng ghi vết các hành vi vi phạm an ninh (`401`, `403`, `400`, `415`, `429`) và truy cập hợp lệ.

| Dashboard Bản V1 (Port 5001 - Baseline) | Dashboard Bản V2 (Port 5002 - Secure) |
| :---: | :---: |
| ![V1 Dashboard Overview](./screenshots/01_V1_Dashboard_Overview.png) | ![V2 Dashboard Overview](./screenshots/02_V2_Dashboard_Overview.png) |

---

## 🎯 3. PHƯƠNG PHÁP THỰC NGHIỆM TRỰC TIẾP TRÊN GIAO DIỆN THỰC TẾ

Thay vì sử dụng các kịch bản dựng sẵn, toàn bộ quá trình kiểm thử được thực hiện **trực tiếp qua các ô nhập liệu, nút bấm và luồng thao tác tự nhiên** của ứng dụng:

### 🔹 Kịch bản 1 (KB1): Lỗ hổng Xác thực (Broken Authentication - `GET /api/orders`)
- **Thao tác trên Web V1:** Tại Cổng đăng nhập, bấm *"Bỏ qua đăng nhập / Vào chế độ Nặc danh"* &rarr; Hệ thống vẫn cho phép lấy và xem toàn bộ danh sách đơn hàng nội bộ (`HTTP 200 OK - BỊ KHAI THÁC`).
- **Thao tác trên Web V2:** Tại Cổng đăng nhập, bấm vào chế độ nặc danh &rarr; Yêu cầu lấy dữ liệu bị ASP.NET Core Authentication Middleware từ chối ngay với mã `HTTP 401 Unauthorized` (`AN TOÀN`).

### 🔹 Kịch bản 2 (KB2): Lỗ hổng Phân quyền Vai trò Quản trị (Broken RBAC - `GET /api/admin/users`)
- **Thao tác trên Web V1:** Đăng nhập tài khoản khách hàng thường `userA`, chuyển sang tab *"👥 Quản Trị Người Dùng"* &rarr; Lộ toàn bộ danh sách tài khoản kèm mật khẩu Plaintext (`HTTP 200 OK - BỊ KHAI THÁC`).
- **Thao tác trên Web V2:** Đăng nhập `userA`, chuyển sang tab *"👥 Quản Trị Người Dùng"* &rarr; Policy `AdminOnly` chặn đứng yêu cầu với mã `HTTP 403 Forbidden` (`AN TOÀN`). Khi đăng nhập tài khoản `admin`, dữ liệu hiển thị an toàn với mật khẩu đã băm BCrypt.

### 🔹 Kịch bản 3 (KB3): Kiểm soát Truy cập Đối tượng (BOLA / IDOR - `GET /api/orders/{id}`)
- **Thao tác trên Web V1:** Đăng nhập `userA` (chỉ sở hữu đơn #1, #2, #5), tại ô *"Xem Đơn hàng theo ID"* nhập ID `3` (thuộc về `userB`) &rarr; V1 trả về toàn bộ thông tin chi tiết đơn hàng của `userB` (`HTTP 200 OK - BỊ KHAI THÁC`).
- **Thao tác trên Web V2:** Đăng nhập `userA`, nhập ID `3` &rarr; V2 đối chiếu claim `userId` trong JWT với chủ sở hữu đơn hàng trong CSDL và chặn lại với mã `HTTP 403 Forbidden` (`AN TOÀN`).

### 🔹 Kịch bản 4 (KB4): Kiểm soát Đầu vào & Chống SQL Injection (`GET /api/orders/search`)
- **Thao tác trên Web V1:** Tại ô *"Tìm kiếm Đơn hàng"*, nhập chuỗi tấn công: `' OR 1=1 --` rồi bấm **Tìm** &rarr; Câu lệnh ghép chuỗi SQL thô bị sai lệch mệnh đề `WHERE`, đổ sạch toàn bộ cơ sở dữ liệu (`HTTP 200 OK - BỊ KHAI THÁC`).
- **Thao tác trên Web V2:** Nhập `' OR 1=1 --` rồi bấm **Tìm** &rarr; Bộ lọc Regex và cơ chế Parameterized LINQ phát hiện ký tự nguy hiểm, chặn ngay với mã `HTTP 400 Bad Request` (`AN TOÀN`).

---

## ⚡ 4. BỐN CƠ CHẾ PHÒNG THỦ CHIỀU SÂU MỞ RỘNG (KB5 – KB8)

| Kịch bản | Tên cơ chế | Hành vi trên Web V1 (Lỗ hổng) | Cơ chế phòng ngự trên Web V2 (An toàn) |
| :---: | :--- | :--- | :--- |
| **KB5** | **Token Revocation (JTI Blacklist)** | Khi Logout, Token cũ vẫn tiếp tục sử dụng để gọi API bình thường (`200 OK`). | Đưa `jti` vào `ITokenBlacklistService` khi Logout; `SecurityAuditMiddleware` chặn Token cũ với mã **`401 Unauthorized`**. |
| **KB6** | **Business Logic & Anti-Mass Assignment** | Cho phép gửi đơn với tổng tiền âm `-999,999đ`; cho phép client tự gán `UserId` của người khác. | Kiểm tra `TotalAmount > 0`, bắt buộc trích xuất `UserId` từ Claims xác thực; trả về **`400 Bad Request`** nếu vi phạm. |
| **KB7** | **Magic Bytes File Upload Validation** | Chấp nhận upload file giả mạo đuôi kép `shell.jpg.aspx` hoặc script độc hại. | Kiểm tra chữ ký byte đầu file (`FF D8 FF`, `89 50 4E 47`, `%PDF`); từ chối file độc hại với mã **`415 Unsupported Media Type`**. |
| **KB8** | **Rate Limiting & Anti-Brute-Force** | Cho phép spam liên tục hàng ngàn request mà không hề bị giới hạn tần suất. | `IRateLimitingService` áp dụng thuật toán Sliding Window (giới hạn 5 req/5s); trả về **`429 Too Many Requests`** kèm header `Retry-After`. |

---

## 📜 5. KIỂM THỬ TRỰC TIẾP QUA SWAGGER UI

Cả 2 dịch vụ đều tích hợp sẵn tài liệu tương tác Swagger UI chuẩn OpenAPI:
* **Web V1 Swagger:** [http://localhost:5001/swagger](http://localhost:5001/swagger) — Cho phép gọi trực tiếp các API không bảo mật để kiểm chứng kết quả đối chứng.
* **Web V2 Swagger:** [http://localhost:5002/swagger](http://localhost:5002/swagger) — Tích hợp nút **`Authorize 🔒`** cho phép dán JWT Bearer token để kiểm thử quyền truy cập.

---

## 📂 6. CẤU TRÚC SOLUTION & HƯỚNG DẪN KHỞI CHẠY

```
ĐACN/
├── SecurityLab.sln                     # File Solution C# (.NET 8.0)
├── run_all.ps1 / run_all.bat           # Script khởi chạy đồng thời cả 2 Web V1 (5001) & V2 (5002)
├── test_api.ps1 / test_api.bat         # Script PowerShell kiểm thử tự động 17 tiêu chí (PASS 17/17)
├── record_demo.ps1                     # Script tự động chạy demo và quay video Full HD
├── record_two_videos.ps1               # Script quay 2 video demo riêng biệt cho V1 và V2
├── WebApi.V1_Vulnerable/               # [WEB V1 - RED TEAM] (http://localhost:5001)
│   └── README.md                       # Tài liệu kỹ thuật chi tiết của Web V1
├── WebApi.V2_Secure/                   # [WEB V2 - BLUE TEAM] (http://localhost:5002)
│   └── README.md                       # Tài liệu kỹ thuật chi tiết của Web V2
├── screenshots/                        # Bộ hình ảnh thực nghiệm Full HD
└── videos/                             # Video thực nghiệm demo
```

### Cách khởi chạy hệ thống:
1. **Khởi động đồng thời cả 2 phiên bản:**
   ```powershell
   .\run_all.ps1
   ```
2. **Chạy kiểm thử tự động toàn diện:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\test_api.ps1
   ```
3. **Danh sách tài khoản mẫu (Seed Data):**
   - **Quản trị viên (`Admin`):** `admin` / `Admin@123`
   - **Nhân viên (`NhanVien`):** `staff1` / `Staff@123`
   - **Khách hàng A (`NguoiDung` - Đơn #1, #2, #5):** `userA` / `UserA@123`
   - **Khách hàng B (`NguoiDung` - Đơn #3, #4):** `userB` / `UserB@123`
