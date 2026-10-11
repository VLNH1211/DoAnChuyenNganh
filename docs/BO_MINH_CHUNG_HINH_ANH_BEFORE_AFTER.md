# BỘ MINH CHỨNG HÌNH ẢNH THỰC NGHIỆM ĐỐI CHIẾU BEFORE / AFTER
## ĐỀ TÀI: XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO ỨNG DỤNG WEB/API DỰA TRÊN XÁC THỰC, PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP

---

### THÔNG TIN HỌC THUẬT & SINH VIÊN THỰC HIỆN
- **Sinh viên thực hiện:** **Võ Lê Nhật Hoàng**
- **Mã số sinh viên (MSSV):** **2387700017**
- **Lớp học phần:** **23DATA1**
- **Ngành đào tạo:** **An toàn thông tin**
- **Khoa / Viện:** Viện Công nghệ Thông tin - Trường Đại học Công nghệ TP.HCM (**HUTECH**)
- **Môi trường thực nghiệm:**
  - **Phiên bản V1 (Chưa bảo mật - Baseline):** `http://localhost:5001` (Database: `SecurityLab_V1`)
  - **Phiên bản V2 (Đã bảo mật 4 lớp - Hardened):** `http://localhost:5002` (Database: `SecurityLab_V2`)
  - **Độ phân giải chuẩn thu nhận hình ảnh:** Full HD `1920x1080`

---

## MỤC LỤC BỘ MINH CHỨNG HÌNH ẢNH

1. [Phần 1: Tổng quan Giao diện Bảng điều khiển Thực nghiệm](#phan-1-tong-quan-giao-dien-bang-dieu-khien-thuc-nghiem)
   - *Hình 1.1: Giao diện Cảnh báo Đỏ trên Bản V1 (`01_V1_Dashboard_Overview.png`)*
   - *Hình 1.2: Giao diện An toàn Xanh trên Bản V2 (`02_V2_Dashboard_Overview.png`)*
2. [Phần 2: Cặp minh chứng KB1 - Kiểm soát Xác thực (Authentication Control)](#phan-2-cap-minh-chung-kb1---kiem-soat-xac-thuc-authentication-control)
   - *Hình 2.1: Before - Bản V1 cho phép nặc danh truy xuất dữ liệu (200 OK) (`03_KB1_Before_V1_Unauthenticated_200.png`)*
   - *Hình 2.2: After - Bản V2 chặn đứng yêu cầu chưa xác thực (401 Unauthorized) (`04_KB1_After_V2_Unauthorized_401.png`)*
   - *Hình 2.3: After - Bản V2 xác thực thành công JWT Bearer & giải mã Claims (`05_KB1_After_V2_ValidToken_UserA_200.png`)*
3. [Phần 3: Cặp minh chứng KB2 - Phân quyền Vai trò Quản trị (Role-Based Access Control - RBAC)](#phan-3-cap-minh-chung-kb2---phan-quyen-vai-tro-quan-tri-role-based-access-control---rbac)
   - *Hình 3.1: Before - Bản V1 để lộ API Quản trị & mật khẩu thô cho User thường (`06_KB2_Before_V1_BrokenRBAC_200.png`)*
   - *Hình 3.2: After - Bản V2 áp dụng Policy AdminOnly chặn đứng User thường (403 Forbidden) (`07_KB2_After_V2_RBAC_Blocked_403.png`)*
   - *Hình 3.3: After - Bản V2 chỉ cho phép Admin truy cập hợp lệ & ẩn mật khẩu BCrypt (`08_KB2_After_V2_Admin_Access_200.png`)*
4. [Phần 4: Cặp minh chứng KB3 - Kiểm soát Truy cập Đối tượng BOLA / IDOR (Object-Level Access Control)](#phan-4-cap-minh-chung-kb3---kiem-soat-truy-cap-doi-tuong-bola--idor-object-level-access-control)
   - *Hình 4.1: Before - Bản V1 để lộ đơn hàng người khác qua IDOR (200 OK) (`09_KB3_Before_V1_IDOR_Leaked_200.png`)*
   - *Hình 4.2: After - Bản V2 chặn truy cập trái phép dữ liệu đối tượng (403 Forbidden) (`10_KB3_After_V2_IDOR_Blocked_403.png`)*
5. [Phần 5: Cặp minh chứng KB4 - Kiểm soát Toàn vẹn Dữ liệu & Phòng chống SQL Injection](#phan-5-cap-minh-chung-kb4---kiem-soat-toan-ven-du-lieu--phong-chong-sql-injection)
   - *Hình 5.1: Before - Bản V1 bị tấn công SQL Injection trích xuất toàn bộ dữ liệu (200 OK) (`11_KB4_Before_V1_SQLi_Dumped_200.png`)*
   - *Hình 5.2: After - Bản V2 chặn chuỗi SQL Injection độc hại (400 Bad Request) (`12_KB4_After_V2_SQLi_Blocked_400.png`)*
6. [Phần 6: Minh chứng Bảng tổng hợp Thực nghiệm Tự động 4 Kịch bản (Run All KB)](#phan-6-minh-chung-bang-tong-hop-thuc-nghiem-tu-dong-4-kich-ban-run-all-kb)
   - *Hình 6.1: Bảng tổng hợp V1 báo động 4/4 Lỗ hổng nghiêm trọng (`13a_V1_RunAll.png`)*
   - *Hình 6.2: Bảng tổng hợp V2 xác nhận 4/4 Cơ chế bảo mật hoạt động an toàn (`13b_V2_RunAll.png`)*
7. [Phần 7: Minh chứng Hệ thống Nhật ký Kiểm toán An ninh (Security Audit Logging)](#phan-7-minh-chung-he-thong-nhat-ky-kiem-toan-an-ninh-security-audit-logging)
   - *Hình 7.1: Bảng nhật ký AuditLogs và thống kê phát hiện xâm nhập (`14_V2_Security_Audit_Logs_Table.png`)*

---

## PHẦN 1: TỔNG QUAN GIAO DIỆN BẢNG ĐIỀU KHIỂN THỰC NGHIỆM

Giao diện Web Cyber Security Dashboard được thiết kế theo chuẩn nhận diện màu sắc đặc thù:
- **Bản V1 (`localhost:5001`):** Tông màu chủ đạo **Đỏ Cảnh báo (Red Alert: `#ef4444` / `#dc2626`)**, huy hiệu trạng thái hiển thị rõ: `⚠ PHIÊN BẢN V1: CHƯA BẢO MẬT (VULNERABLE BASELINE)`.
- **Bản V2 (`localhost:5002`):** Tông màu chủ đạo **Xanh Lục An Toàn (Cyber Green: `#10b981` / `#059669`)**, huy hiệu trạng thái hiển thị rõ: `🛡 PHIÊN BẢN V2: ĐÃ BẢO MẬT 4 LỚP (HARDENED SECURITY)`.

### Hình 1.1: Giao diện tổng quan Bản V1 - Chưa bảo mật
![01_V1_Dashboard_Overview](../screenshots/01_V1_Dashboard_Overview.png)
*Chú thích Hình 1.1: Tổng quan bảng điều khiển bản V1 tại `http://localhost:5001`. Thanh điều hướng cảnh báo màu đỏ, thanh chuyển đổi nhanh người dùng (Quick Login Switcher), bộ mô phỏng 4 kịch bản tấn công 1-Click Simulator và khung giám sát HTTP Inspector theo thời gian thực.*

---

### Hình 1.2: Giao diện tổng quan Bản V2 - Đã bảo mật 4 lớp
![02_V2_Dashboard_Overview](../screenshots/02_V2_Dashboard_Overview.png)
*Chú thích Hình 1.2: Tổng quan bảng điều khiển bản V2 tại `http://localhost:5002`. Thanh điều hướng chuẩn màu xanh bảo mật, tích hợp bộ giải mã Claims JWT Token trực quan, bảng dữ liệu đơn hàng phân lập theo quyền sở hữu và khung giám sát an ninh Audit Logs.*

---

## PHẦN 2: CẶP MINH CHỨNG KB1 - KIỂM SOÁT XÁC THỰC (AUTHENTICATION CONTROL)

- **Mục tiêu kiểm thử:** Kiểm tra khả năng chặn truy cập nặc danh (Anonymous) vào tài nguyên đơn hàng nhạy cảm (`GET /api/orders`).
- **Tiêu chuẩn an toàn:** Yêu cầu phải có JWT Bearer Token hợp lệ theo chuẩn RFC 7519; nếu không có Token, API bắt buộc trả về mã `401 Unauthorized`.

| Tiêu chí | Phiên bản V1 (Chưa bảo mật) | Phiên bản V2 (Đã bảo mật) |
| :--- | :--- | :--- |
| **Endpoint** | `GET /api/orders` | `GET /api/orders` |
| **Header** | Không gửi Header `Authorization` | Không gửi Header `Authorization` |
| **Mã HTTP trả về** | **`200 OK` (Lỗ hổng Broken Authentication)** | **`401 Unauthorized` (Đã chặn an toàn)** |
| **Dữ liệu trả về** | Rò rỉ toàn bộ 5 đơn hàng trong CSDL cho người lạ | Thông báo lỗi xác thực: `Chưa xác thực hoặc Token không hợp lệ!` |

### Hình 2.1: [BEFORE] Bản V1 cho phép người dùng nặc danh truy xuất toàn bộ đơn hàng (200 OK)
![03_KB1_Before_V1_Unauthenticated_200](../screenshots/03_KB1_Before_V1_Unauthenticated_200.png)
*Chú thích Hình 2.1: Minh chứng lỗ hổng KB1 trên bản V1. Khi ở trạng thái Chưa đăng nhập (Anonymous), thực hiện lệnh `GET /api/orders` không có Token vẫn nhận được mã `200 OK` kèm danh sách đơn hàng chi tiết.*

---

### Hình 2.2: [AFTER] Bản V2 chặn đứng yêu cầu nặc danh với mã 401 Unauthorized
![04_KB1_After_V2_Unauthorized_401](../screenshots/04_KB1_After_V2_Unauthorized_401.png)
*Chú thích Hình 2.2: Minh chứng cơ chế phòng thủ KB1 trên bản V2. Khi không có Token Bearer, hệ thống từ chối phục vụ ngay lập tức với mã `401 Unauthorized`, Live HTTP Inspector đổi màu vàng cảnh báo vi phạm.*

---

### Hình 2.3: [AFTER SUCCESS] Bản V2 xác thực thành công JWT Bearer Token của userA
![05_KB1_After_V2_ValidToken_UserA_200](../screenshots/05_KB1_After_V2_ValidToken_UserA_200.png)
*Chú thích Hình 2.3: Khi người dùng `userA` đăng nhập thành công, hệ thống cấp JWT Token có chữ ký số HMAC-SHA256, giải mã rõ các Claims (`sub: userA`, `role: NguoiDung`, `UserId: 3`) và API chỉ trả về đúng 3 đơn hàng (#1, #2, #5) thuộc quyền sở hữu của userA với mã `200 OK`.*

---

## PHẦN 3: CẶP MINH CHỨNG KB2 - PHÂN QUYỀN VAI TRÒ QUẢN TRỊ (ROLE-BASED ACCESS CONTROL - RBAC)

- **Mục tiêu kiểm thử:** Ngăn chặn người dùng có vai trò thông thường (`NguoiDung`) truy cập vào các chức năng quản trị viên (`GET /api/admin/users`).
- **Nguyên tắc áp dụng:** Nguyên tắc đặc quyền tối thiểu (Principle of Least Privilege) và Phân quyền theo chức năng (RBAC Policy `AdminOnly`).

| Tiêu chí | Phiên bản V1 (Chưa bảo mật) | Phiên bản V2 (Đã bảo mật) |
| :--- | :--- | :--- |
| **Endpoint** | `GET /api/admin/users` | `GET /api/admin/users` |
| **Tài khoản kiểm thử** | `userA` (Vai trò: `NguoiDung`) | `userA` (Vai trò: `NguoiDung`) |
| **Mã HTTP trả về** | **`200 OK` (Lỗ hổng Broken RBAC / BFLA)** | **`403 Forbidden` (Đã chặn an toàn)** |
| **Hậu quả / Kết quả** | Lộ toàn bộ người dùng và mật khẩu dạng văn bản thô (Plaintext) | Bị Policy `AdminOnly` chặn đứng hoàn toàn, ghi vết vào Audit Log |

### Hình 3.1: [BEFORE] Bản V1 để lộ chức năng Quản trị và mật khẩu Plaintext cho userA (200 OK)
![06_KB2_Before_V1_BrokenRBAC_200](../screenshots/06_KB2_Before_V1_BrokenRBAC_200.png)
*Chú thích Hình 3.1: Minh chứng lỗ hổng Broken RBAC trên bản V1. Tài khoản khách hàng thông thường `userA` vẫn truy cập thành công tab Quản trị người dùng `GET /api/admin/users` (`200 OK`), bảng dữ liệu hiển thị trực tiếp mật khẩu plaintext (`Admin@123`, `Staff@123`).*

---

### Hình 3.2: [AFTER] Bản V2 chặn đứng yêu cầu trái quyền của userA với mã 403 Forbidden
![07_KB2_After_V2_RBAC_Blocked_403](../screenshots/07_KB2_After_V2_RBAC_Blocked_403.png)
*Chú thích Hình 3.2: Minh chứng cơ chế bảo mật RBAC trên bản V2. Khi `userA` cố tình gọi API `/api/admin/users`, ASP.NET Core Authorization Policy `AdminOnly` nhận diện vai trò `NguoiDung` không thỏa mãn và trả về mã `403 Forbidden`.*

---

### Hình 3.3: [AFTER SUCCESS] Bản V2 cho phép Quản trị viên (admin) truy cập hợp lệ và ẩn mật khẩu BCrypt
![08_KB2_After_V2_Admin_Access_200](../screenshots/08_KB2_After_V2_Admin_Access_200.png)
*Chú thích Hình 3.3: Khi đăng nhập bằng tài khoản Quản trị `admin` (Role: `Admin`), API `/api/admin/users` trả về mã `200 OK`. Cột mật khẩu hiển thị chuỗi đã băm BCrypt an toàn kèm nhãn xanh `[ĐÃ BĂM BCRYPT / BẢO MẬT]`, không để lộ mật khẩu gốc.*

---

## PHẦN 4: CẶP MINH CHỨNG KB3 - KIỂM SOÁT TRUY CẬP ĐỐI TƯỢNG BOLA / IDOR (OBJECT-LEVEL ACCESS CONTROL)

- **Mục tiêu kiểm thử:** Ngăn chặn hành vi thao túng tham số định danh (`id=3`) để xem trộm đơn hàng của khách hàng khác (`BOLA/IDOR - Broken Object Level Authorization`).
- **Nguyên tắc áp dụng:** Thuộc tính hóa kiểm soát truy cập (ABAC) - Đối chiếu `UserId` trong dữ liệu đơn hàng với `ClaimTypes.NameIdentifier` trong JWT Token của người gửi yêu cầu.

| Tiêu chí | Phiên bản V1 (Chưa bảo mật) | Phiên bản V2 (Đã bảo mật) |
| :--- | :--- | :--- |
| **Endpoint** | `GET /api/orders/3` | `GET /api/orders/3` |
| **Người thực hiện** | `userA` (Chủ sở hữu đơn #1, #2, #5) | `userA` (Chủ sở hữu đơn #1, #2, #5) |
| **Đối tượng truy cập** | Đơn hàng `#3` của `userB` (Trần Thị Bích) | Đơn hàng `#3` của `userB` (Trần Thị Bích) |
| **Mã HTTP trả về** | **`200 OK` (Lỗ hổng BOLA / IDOR)** | **`403 Forbidden` (Đã chặn an toàn)** |
| **Kết quả** | `userA` đọc được toàn bộ chi tiết đơn hàng và giá trị tiền của `userB` | Bị từ chối truy cập: `Bạn không có quyền truy cập đơn hàng này!` |

### Hình 4.1: [BEFORE] Bản V1 cho phép userA xem trộm đơn hàng #3 của userB (200 OK)
![09_KB3_Before_V1_IDOR_Leaked_200](../screenshots/09_KB3_Before_V1_IDOR_Leaked_200.png)
*Chú thích Hình 4.1: Minh chứng lỗ hổng IDOR trên bản V1. `userA` gọi `GET /api/orders/3`, hệ thống trả về mã `200 OK` để lộ thông tin đơn hàng 12.500.000 VNĐ của khách hàng `Trần Thị Bích (userB)`.*

---

### Hình 4.2: [AFTER] Bản V2 chặn đứng nỗ lực IDOR của userA với mã 403 Forbidden
![10_KB3_After_V2_IDOR_Blocked_403](../screenshots/10_KB3_After_V2_IDOR_Blocked_403.png)
*Chú thích Hình 4.2: Minh chứng cơ chế chống IDOR trên bản V2. `userA` cố tình gọi xem đơn hàng `#3` của `userB`, hệ thống lập tức chặn đứng với mã `403 Forbidden`, bảo vệ trọn vẹn quyền riêng tư dữ liệu người dùng.*

---

## PHẦN 5: CẶP MINH CHỨNG KB4 - KIỂM SOÁT TOÀN VẸN DỮ LIỆU & PHÒNG CHỐNG SQL INJECTION

- **Mục tiêu kiểm thử:** Ngăn chặn tấn công chèn mã SQL (`SQL Injection`) thông qua tham số tìm kiếm (`GET /api/orders/search?keyword=...`).
- **Payload tấn công:** `' OR 1=1 --`
- **Cơ chế bảo vệ áp dụng trên V2:**
  1. Kiểm tra định dạng đầu vào bằng biểu thức chính quy (Regex Pattern Sanitization).
  2. Bắt buộc sử dụng truy vấn tham số hóa (Parameterized Query) thông qua Entity Framework Core LINQ (`Where(o => o.CustomerName.Contains(keyword))`).

| Tiêu chí | Phiên bản V1 (Chưa bảo mật) | Phiên bản V2 (Đã bảo mật) |
| :--- | :--- | :--- |
| **Endpoint** | `GET /api/orders/search?keyword=' OR 1=1 --` | `GET /api/orders/search?keyword=' OR 1=1 --` |
| **Phương thức xử lý SQL** | Nối chuỗi SQL thô (`FromSqlRaw($"SELECT ... '{keyword}'")`) | Kiểm tra Regex lọc ký tự độc hại + EF Core LINQ Parameterized |
| **Mã HTTP trả về** | **`200 OK` (Lỗ hổng SQL Injection)** | **`400 Bad Request` (Đã chặn an toàn)** |
| **Hậu quả / Kết quả** | Vô hiệu hóa mệnh đề `WHERE`, dump toàn bộ cơ sở dữ liệu | Phát hiện chuỗi tiêm mã độc hại, trả về mã 400 và không làm gián đoạn CSDL |

### Hình 5.1: [BEFORE] Bản V1 bị tấn công SQL Injection và dump toàn bộ dữ liệu (200 OK)
![11_KB4_Before_V1_SQLi_Dumped_200](../screenshots/11_KB4_Before_V1_SQLi_Dumped_200.png)
*Chú thích Hình 5.1: Minh chứng lỗ hổng SQL Injection trên bản V1. Payload `' OR 1=1 --` làm sai lệch logic truy vấn, trả về mã `200 OK` và đổ ra toàn bộ danh sách đơn hàng vượt ngoài phạm vi tìm kiếm.*

---

### Hình 5.2: [AFTER] Bản V2 chặn đứng chuỗi SQL Injection độc hại với mã 400 Bad Request
![12_KB4_After_V2_SQLi_Blocked_400](../screenshots/12_KB4_After_V2_SQLi_Blocked_400.png)
*Chú thích Hình 5.2: Minh chứng cơ chế phòng thủ SQLi trên bản V2. Bộ lọc dữ liệu đầu vào nhận diện ký tự `' OR 1=1 --` có dấu hiệu tiêm mã, hệ thống từ chối xử lý với mã `400 Bad Request` an toàn, không bị lỗi 500 sập dịch vụ.*

---

## PHẦN 6: MINH CHỨNG BẢNG TỔNG HỢP THỰC NGHIỆM TỰ ĐỘNG 4 KỊCH BẢN (RUN ALL KB)

Chức năng **"Chạy toàn bộ 4 kịch bản (Run All KB)"** tự động kích hoạt tuần tự cả 4 kịch bản kiểm thử (KB1 -> KB2 -> KB3 -> KB4) và hiển thị kết quả trên các thẻ trạng thái:

### Hình 6.1: Bảng tổng hợp thực nghiệm Bản V1 - Cảnh báo 4/4 Lỗ hổng
![13a_V1_RunAll](../screenshots/13a_V1_RunAll.png)
*Chú thích Hình 6.1: Kết quả chạy tự động toàn diện trên Bản V1. Cả 4 thẻ kịch bản đều chuyển sang màu ĐỎ `[LỖ HỔNG XÁC THỰC]`, `[LỖ HỔNG PHÂN QUYỀN]`, `[LỖ HỔNG IDOR]`, `[LỖ HỔNG SQL INJECTION]` kèm mã `200 OK` rò rỉ dữ liệu.*

---

### Hình 6.2: Bảng tổng hợp thực nghiệm Bản V2 - Xác nhận 4/4 An toàn
![13b_V2_RunAll](../screenshots/13b_V2_RunAll.png)
*Chú thích Hình 6.2: Kết quả chạy tự động toàn diện trên Bản V2. Cả 4 thẻ kịch bản đều chuyển sang màu XANH LỤC `[ĐÃ BẢO VỆ AN TOÀN]` với các mã chặn tương ứng: `KB1: 401 Unauthorized`, `KB2: 403 Forbidden`, `KB3: 403 Forbidden`, `KB4: 400 Bad Request`.*

---

## PHẦN 7: MINH CHỨNG HỆ THỐNG NHẬT KÝ KIỂM TOÁN AN NINH (SECURITY AUDIT LOGGING)

- **Mục tiêu:** Ghi nhận và lưu vết toàn bộ các hành vi truy cập trái phép, lỗi xác thực và tấn công vào bảng CSDL `AuditLogs`.
- **Cơ chế triển khai:** `SecurityAuditMiddleware` kết hợp `AuditLogService` tự động bắt các mã trạng thái bất thường (`401`, `403`, `400`) và ghi lại địa chỉ IP, tài khoản, vai trò, thời gian và endpoint bị tấn công.

### Hình 7.1: Bảng nhật ký kiểm toán an ninh trên Bản V2 (localhost:5002)
![14_V2_Security_Audit_Logs_Table](../screenshots/14_V2_Security_Audit_Logs_Table.png)
*Chú thích Hình 7.1: Bảng Nhật ký Kiểm toán An ninh (Security Audit Logs) trên Bản V2. Bảng hiển thị đầy đủ các sự kiện vi phạm an ninh vừa xảy ra: các yêu cầu bị chặn 401 (Anonymous), 403 (userA vi phạm RBAC/IDOR), 400 (SQL Injection) và các thẻ thống kê tổng hợp số vụ vi phạm được ngăn chặn thành công.*

---


---

## PHẦN 8: CẶP MINH CHỨNG KB5 - QUẢN LÝ VÒNG ĐỜI JWT & THU HỒI TOKEN KHI ĐĂNG XUẤT (TOKEN REVOCATION / JTI BLACKLIST)

- **Mục tiêu kiểm thử:** Ngăn chặn tấn công tái sử dụng phiên đăng nhập (Session / Token Replay Attack) sau khi người dùng đã chủ động Đăng xuất (`POST /api/auth/logout`).
- **Cơ chế triển khai trên V2:**
  1. Khi cấp Token JWT, hệ thống bổ sung Claim duy nhất `jti` (JWT ID - Unique Identifier Guid).
  2. Khi gọi `POST /api/auth/logout`, `TokenBlacklistService` đưa mã `jti` vào danh sách thu hồi (In-Memory Blacklist với TTL tự hủy theo thời gian hết hạn của Token).
  3. `SecurityAuditMiddleware` kiểm tra `jti` của từng yêu cầu gửi đến; nếu token đã bị thu hồi, lập tức ngắt phiên và trả về **`401 Unauthorized (Token Revoked)`**.

| Tiêu chí | Phiên bản V1 (Chưa bảo mật) | Phiên bản V2 (Đã bảo mật) |
| :--- | :--- | :--- |
| **Hành vi Đăng xuất** | Chỉ trả thông báo thành công, không lưu vết thu hồi | Đưa `jti` vào `TokenBlacklistService` |
| **Phát lại Token cũ (`GET /api/auth/me`)** | **`200 OK` (Lỗ hổng Token Replay)** | **`401 Unauthorized` (Đã chặn đứng Token Replay)** |
| **Thông báo lỗi** | Vẫn cấp phát danh tính người dùng như bình thường | `Token đã bị thu hồi do người dùng đã đăng xuất!` |

---

## PHẦN 9: CẶP MINH CHỨNG KB6 - CHỐNG LẠM DỤNG LOGIC NGHIỆP VỤ & MASS ASSIGNMENT & IDOR SỬA/XÓA

- **Mục tiêu kiểm thử:** Bảo vệ toàn vẹn logic nghiệp vụ đơn hàng (Business Logic Flaw), chống client tự gán trường nhạy cảm (Mass Assignment) và chống sửa/xóa trái phép đơn của người khác (IDOR on PUT/DELETE).
- **Kịch bản kiểm thử:**
  1. Client gửi yêu cầu tạo đơn hàng với giá trị âm `totalAmount: -999999` hoặc tự ép `status: "Completed"`, `userId: 4` (thuộc tài khoản khác).
  2. `userA` cố tình gửi lệnh `DELETE /api/orders/3` để xóa đơn hàng của `userB`.

| Tiêu chí | Phiên bản V1 (Chưa bảo mật) | Phiên bản V2 (Đã bảo mật) |
| :--- | :--- | :--- |
| **Tạo đơn tiền âm (-999k)** | **`200 OK` (Lỗ hổng Business Logic)** | **`400 Bad Request` (Chặn số tiền không hợp lệ)** |
| **Client tự gán `userId` & `status`** | **`200 OK` (Lỗ hổng Mass Assignment)** | **Tự động ép `UserId` từ JWT Claims; chặn `403` nếu không phải Admin** |
| **`userA` xóa đơn #3 của `userB`** | **`200 OK` (Lỗ hổng IDOR khi Xóa)** | **`403 Forbidden` (Chặn hành vi xóa trái phép đơn người khác)** |

---

## PHẦN 10: CẶP MINH CHỨNG KB7 - BẢO MẬT QUY TRÌNH UPLOAD FILE CHỨNG TỪ & NHẬN DIỆN MAGIC BYTES

- **Mục tiêu kiểm thử:** Ngăn chặn tin tặc tải lên tệp thực thi độc hại (Webshell `.aspx`, `.sh`) ngụy trang dưới đuôi ảnh, hoặc sử dụng thủ thuật Path Traversal (`../../evil.sh`) để ghi đè tệp hệ thống.
- **Cơ chế triển khai trên V2:**
  1. Kiểm tra quyền sở hữu đơn hàng trước khi cho phép tải lên chứng từ đính kèm.
  2. Kiểm tra phần mở rộng tệp hợp lệ trong whitelist (`.jpg`, `.jpeg`, `.png`, `.pdf`) và chống double extension (`webshell.jpg.aspx`).
  3. **Kiểm tra Magic Bytes (Chữ ký nhị phân thực tế của tệp):**
     - JPEG/JPG: Bắt đầu bằng chuỗi byte `FF D8 FF`
     - PNG: Bắt đầu bằng chuỗi byte `89 50 4E 47`
     - PDF: Bắt đầu bằng chuỗi byte `25 50 44 46`
  4. Đổi tên lưu trữ thành chuỗi GUID ngẫu nhiên an toàn, ngăn chặn tấn công Path Traversal.

| Tiêu chí | Phiên bản V1 (Chưa bảo mật) | Phiên bản V2 (Đã bảo mật) |
| :--- | :--- | :--- |
| **Upload file `webshell.jpg.aspx`** | **`200 OK` (Lỗ hổng RCE / Unrestricted Upload)** | **`400 Bad Request / 415 Unsupported Media Type`** |
| **File giả mạo (đuôi `.jpg` nhưng nội dung text script)** | **`200 OK` (Chỉ kiểm tra Content-Type giả)** | **`400 Bad Request` (Vi phạm Magic Bytes Signature)** |

---

## PHẦN 11: CẶP MINH CHỨNG KB8 - RATE LIMITING & CHỐNG BRUTE-FORCE / API ABUSE (429 TOO MANY REQUESTS)

- **Mục tiêu kiểm thử:** Chống lại các cuộc tấn công dò quét mật khẩu (Brute-Force Attack) vào endpoint đăng nhập và chống vét cạn tài nguyên (Denial of Service / API Abuse).
- **Cơ chế triển khai trên V2:**
  1. Cơ chế Brute-force Lockout trên `/api/auth/login`: Tự động khóa tạm thời IP/Tài khoản nếu đăng nhập sai quá 5 lần liên tiếp.
  2. Thuật toán Sliding Window Rate Limiting trên `/api/orders/rate-test`: Giới hạn tối đa 5 yêu cầu trong 5 giây. Nếu vượt ngưỡng, chặn ngay với mã **`429 Too Many Requests`** kèm Header `Retry-After: 5`.

| Tiêu chí | Phiên bản V1 (Chưa bảo mật) | Phiên bản V2 (Đã bảo mật) |
| :--- | :--- | :--- |
| **Spam liên tục 8 requests vào API** | Cả 8 lần đều trả về **`200 OK`** | Đến request thứ 6 lập tức bị chặn với **`429 Too Many Requests`** |
| **Header phản hồi** | Không có thông tin giới hạn | Có Header `Retry-After: 5` và ghi sự kiện cảnh báo vào AuditLogs |

---

## PHẦN 12: MINH CHỨNG HTTP SECURITY HEADERS CHUẨN OWASP (OWASP ZAP COMPLIANCE)

- **Mục tiêu:** Bảo vệ máy khách khỏi các cuộc tấn công Clickjacking, MIME-sniffing, Cross-Site Scripting (XSS) và rò rỉ nguồn gốc (Referrer Leakage) khi quét bằng công cụ đánh giá an ninh **OWASP ZAP**.
- **Cấu hình trên V2 (`SecurityAuditMiddleware`):**
  - `X-Content-Type-Options: nosniff` (Chống MIME confusion)
  - `X-Frame-Options: DENY` (Chống Clickjacking)
  - `X-XSS-Protection: 1; mode=block` (Bộ lọc XSS trình duyệt cũ)
  - `Referrer-Policy: strict-origin-when-cross-origin` (Bảo vệ địa chỉ nguồn)
  - `Content-Security-Policy: default-src 'self' 'unsafe-inline' data:;` (Chặn script ngoài trái phép)

### Hình 12.1: [BEFORE] Kết quả chạy liên hoàn KB5–KB8 trên Bản V1 - Phát hiện 4/4 Lỗ hổng mở rộng
![15a_V1_RunAll_Extended_KB5_KB8](../screenshots/15a_V1_RunAll_Extended_KB5_KB8.png)
*Chú thích Hình 12.1: Minh chứng chạy tự động 4 kịch bản mở rộng (`KB5–KB8`) trên bản V1: toàn bộ 4 kịch bản đều báo đỏ `HTTP 200 - LỖ HỔNG`.*

---

### Hình 12.2: [AFTER] Kết quả chạy liên hoàn KB5–KB8 trên Bản V2 - Chặn đứng 4/4 cuộc tấn công
![15b_V2_RunAll_Extended_KB5_KB8](../screenshots/15b_V2_RunAll_Extended_KB5_KB8.png)
*Chú thích Hình 12.2: Minh chứng chạy tự động 4 kịch bản mở rộng (`KB5–KB8`) trên bản V2: toàn bộ 4 cơ chế phòng thủ chiều sâu (`JTI Blacklist 401`, `Business Logic 400`, `Magic Bytes 400/415`, `Rate Limit 429`) hoạt động chính xác.*

---

### Hình 12.3: Kiểm tra trực tiếp bộ 5 OWASP HTTP Security Headers trên Bản V2
![16_V2_OWASP_Security_Headers_LiveCheck](../screenshots/16_V2_OWASP_Security_Headers_LiveCheck.png)
*Chú thích Hình 12.3: Kiểm tra trực tiếp HTTP Response Headers trên Bản V2 xác nhận đầy đủ 5 Header an ninh chuẩn OWASP.*

---

## BẢNG TỔNG KẾT MA TRẬN ĐỐI CHIẾU 17 TIÊU CHÍ THỰC NGHIỆM ĐỒ ÁN (BEFORE / AFTER)

| STT | Kịch bản / Cơ chế phòng thủ | Hành động kiểm thử thực tế | Bản V1 (Chưa bảo mật) | Bản V2 (Đã bảo mật) | Mã lỗi chuẩn V2 | Kết luận Đồ án |
| :---: | :--- | :--- | :---: | :---: | :---: | :---: |
| **1** | **KB1: Kiểm soát xác thực** | Truy cập `GET /api/orders` không có Token | `200 OK` (Lộ dữ liệu) | `401 Unauthorized` | `401` | **ĐẠT CHUẨN** |
| **2** | **KB1: Xác thực hợp lệ** | `userA` gọi `GET /api/orders` với JWT Token | `200 OK` (Lộ toàn bộ) | `200 OK` (Chỉ đơn của userA) | `200` | **ĐẠT CHUẨN** |
| **3** | **KB2: Phân quyền vai trò** | User thường gọi `GET /api/admin/users` | `200 OK` (Lộ mật khẩu) | `403 Forbidden` | `403` | **ĐẠT CHUẨN** |
| **4** | **KB2: Truy cập Quản trị** | Admin gọi `GET /api/admin/users` | `200 OK` (Mật khẩu thô) | `200 OK` (Mật khẩu BCrypt ẩn) | `200` | **ĐẠT CHUẨN** |
| **5** | **KB3: BOLA / IDOR Đọc** | `userA` gọi `GET /api/orders/3` (đơn userB) | `200 OK` (Xem trộm) | `403 Forbidden` | `403` | **ĐẠT CHUẨN** |
| **6** | **KB3: Đọc chính chủ** | `userA` gọi `GET /api/orders/1` | `200 OK` | `200 OK` | `200` | **ĐẠT CHUẨN** |
| **7** | **KB4: SQL Injection** | Payload `' OR 1=1 --` vào tìm kiếm | `200 OK` (Dump CSDL) | `400 Bad Request` | `400` | **ĐẠT CHUẨN** |
| **8** | **KB4: Tìm kiếm an toàn** | Từ khóa hợp lệ `'Dell'` | `200 OK` (Ghép chuỗi) | `200 OK` (LINQ Parameterized) | `200` | **ĐẠT CHUẨN** |
| **9** | **KB5: Thu hồi Token** | Gọi `POST /api/auth/logout` | `200 OK` (Chỉ giả vờ) | Lưu JTI vào Blacklist | `200` | **ĐẠT CHUẨN** |
| **10** | **KB5: Token Replay** | Dùng lại Token cũ gọi `GET /api/auth/me` | `200 OK` (Lộ phiên cũ) | `401 Unauthorized` | `401` | **ĐẠT CHUẨN** |
| **11** | **KB6: Business Logic** | Tạo đơn hàng với số tiền âm `-999,999` | `200 OK` (Tiền âm lọt) | `400 Bad Request` | `400` | **ĐẠT CHUẨN** |
| **12** | **KB6: Mass Assignment** | Client tự gán `UserId: 4`, `Status: Completed` | `200 OK` (Chấp nhận bừa) | Tự bind JWT Claims, chặn sửa | `201` | **ĐẠT CHUẨN** |
| **13** | **KB6: IDOR khi Xóa** | `userA` gọi `DELETE /api/orders/3` | `200 OK` (Xóa trộm đơn) | `403 Forbidden` | `403` | **ĐẠT CHUẨN** |
| **14** | **KB7: Upload Webshell** | Upload `webshell.jpg.aspx` hoặc script thô | `200 OK` (Nguy cơ RCE) | `400 Bad Request / 415` | `400/415` | **ĐẠT CHUẨN** |
| **15** | **KB8: Rate Limiting** | Spam 8 requests vào `/api/orders/rate-test` | `200 OK` cả 8 lần | Lần 6 chặn với `429` | `429` | **ĐẠT CHUẨN** |
| **16** | **OWASP Headers** | Kiểm tra 5 tiêu chí bảo mật HTTP Response | Không có Headers | Gắn đầy đủ 5 Headers | Header | **ĐẠT CHUẨN** |
| **17** | **Nhật ký Kiểm toán** | Lưu vết toàn bộ 401/403/400/429 vào CSDL | Không có | Bảng `AuditLogs` đầy đủ vết | DB Log | **ĐẠT CHUẨN** |

> [!NOTE]
> Toàn bộ kết quả thực nghiệm trên được kiểm thử tự động và xác thực 100% bằng script `test_api.ps1` cùng giao diện Web Dashboard tương tác tại `http://localhost:5001` (Bản V1) và `http://localhost:5002` (Bản V2).
