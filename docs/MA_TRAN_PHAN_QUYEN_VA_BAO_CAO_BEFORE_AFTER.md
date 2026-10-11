# BÁO CÁO KẾT QUẢ THỰC NGHIỆM VÀ MA TRẬN PHÂN QUYỀN HỆ THỐNG WEB/API

---

### THÔNG TIN ĐỀ TÀI VÀ SINH VIÊN THỰC HIỆN
- **Đề tài:** **XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO ỨNG DỤNG WEB/API DỰA TRÊN XÁC THỰC, PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP**
- **Sinh viên thực hiện:** **Võ Lê Nhật Hoàng**
- **Mã số sinh viên (MSSV):** **2387700017**
- **Lớp:** **23DATA1**
- **Ngành đào tạo:** **An toàn thông tin**
- **Đơn vị đào tạo:** Viện Công nghệ Thông tin - Trường Đại học Công nghệ TP.HCM (**HUTECH**)

---

## 1. TỔNG QUAN KIẾN TRÚC VÀ CÔNG NGHỆ HỆ THỐNG THỰC NGHIỆM

Hệ thống được thiết kế theo mô hình thực nghiệm đối chứng (**Before/After Testing Model: Red Team vs Blue Team**) với hai phiên bản độc lập trong cùng một Solution:

```
                                  +---------------------------------------------+
                                  |     Browser / Postman / OWASP ZAP / PS1     |
                                  +----------------------+----------------------+
                                                         |
                                 +-----------------------+-----------------------+
                                 |                                               |
                                 v                                               v
                 +-------------------------------+               +-------------------------------+
                 |     WebApi.V1_Vulnerable      |               |       WebApi.V2_Secure        |
                 |     (Cổng: localhost:5001)    |               |     (Cổng: localhost:5002)    |
                 +---------------+---------------+               +---------------+---------------+
                                 |                                               |
                  Cố ý thiếu các cơ chế bảo mật:                  Áp dụng phòng thủ đa lớp (KB1–KB8):
                  - Không kiểm tra JWT ở KB1                      - Bắt buộc JWT Bearer (KB1 - 401)
                  - Không có Policy Admin ở KB2                   - RBAC Policy AdminOnly (KB2 - 403)
                  - Không kiểm tra chủ đơn ở KB3                  - ABAC / Chặn BOLA-IDOR (KB3 - 403)
                  - Ghép chuỗi SQL thô ở KB4                      - Validation + Parameterized (KB4 - 400)
                  - Không thu hồi JWT khi Logout (KB5)            - JTI Token Blacklist Revocation (KB5 - 401)
                  - Tiền âm, Mass Assignment, IDOR Xóa (KB6)      - Business Logic & Ownership Check (KB6)
                  - Upload file không kiểm tra byte (KB7)         - Magic-Bytes Signature Check (KB7 - 415)
                  - Không giới hạn tần suất gọi API (KB8)         - Anti-Brute-Force & Rate Limit (KB8 - 429)
                                 |                                               |
                                 v                                               v
                 +-------------------------------+               +-------------------------------+
                 | Database: SecurityLab_V1      |               | Database: SecurityLab_V2      |
                 | (SQL Server LocalDB)          |               | (SQL Server LocalDB)          |
                 +-------------------------------+               +-------------------------------+
```

### 1.1 Công nghệ sử dụng
- **Nền tảng:** C# ASP.NET Core Web API (.NET 8.0 LTS)
- **ORM & CSDL:** Entity Framework Core 8.0, Microsoft SQL Server LocalDB (`MSSQLLocalDB`)
- **Cơ chế xác thực & quản lý phiên:** JWT Bearer Token (HMAC-SHA256, RFC 7519 kèm `jti`), BCrypt Password Hashing (`BCrypt.Net-Next`), cơ chế thu hồi Token qua `TokenBlacklistService`
- **Cơ chế phòng thủ chiều sâu:** `RateLimitingService` (Chống Brute-Force & Spam API 429), kiểm tra chữ ký byte thực tế (**Magic Bytes Validation**), tự động gắn **OWASP HTTP Security Headers** (`X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, `Referrer-Policy`, `Content-Security-Policy`)
- **Giám sát & Nhật ký:** `SecurityAuditMiddleware` & Bảng `AuditLogs` (tự động ghi nhận mã `401`, `403`, `400`, `415`, `429`, `200`, `201` kèm Username, Role, IP, Timestamp)

### 1.2 Danh sách tài khoản mẫu (Seed Data)
| Tài khoản (Username) | Mật khẩu mẫu | Vai trò (Role) | Mô tả & Quyền sở hữu dữ liệu |
| :--- | :--- | :--- | :--- |
| `admin` | `Admin@123` | `Admin` | Quản trị viên toàn quyền hệ thống, xem toàn bộ người dùng, đơn hàng và Audit Log. |
| `staff1` | `Staff@123` | `NhanVien` | Nhân viên vận hành; được xem/duyệt toàn bộ đơn hàng nhưng bị cấm truy cập API Quản trị. |
| `userA` | `UserA@123` | `NguoiDung` | Khách hàng thông thường (Võ Lê Nhật Hoàng); chủ sở hữu các đơn hàng: **#1, #2, #5**. |
| `userB` | `UserB@123` | `NguoiDung` | Khách hàng thông thường (Trần Thị Bích); chủ sở hữu các đơn hàng: **#3, #4**. |

---

## 2. MA TRẬN PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP TOÀN DIỆN (ACCESS CONTROL MATRIX)

Ma trận dưới đây mô tả chính sách phân quyền và kiểm soát truy cập trên phiên bản an toàn **`WebApi.V2_Secure`**:

| STT | Phương thức & Endpoint | Chưa đăng nhập (Anonymous) | Người dùng (`userA` / `userB`) | Nhân viên (`staff1`) | Quản trị viên (`admin`) | Cơ chế kiểm soát bảo mật áp dụng |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- |
| 1 | `POST /api/auth/login` | **Cho phép (200)**<br>*(Khóa 429 nếu sai >5 lần)* | **Cho phép (200)** | **Cho phép (200)** | **Cho phép (200)** | **KB1 & KB8:** Xác thực BCrypt + Chống Brute-Force (429) + Cấp JWT kèm `jti` |
| 2 | `POST /api/auth/logout` | **Chặn (401)** | **Cho phép (200)** | **Cho phép (200)** | **Cho phép (200)** | **KB5:** Thu hồi `jti` của JWT vào `TokenBlacklistService` |
| 3 | `GET /api/auth/me` | **Chặn (401)** | **Cho phép (200)**<br>*(Chặn 401 nếu đã Logout)* | **Cho phép (200)** | **Cho phép (200)** | **KB1 & KB5:** Kiểm tra JWT hợp lệ & chưa bị thu hồi trong JTI Blacklist |
| 4 | `GET /api/orders` | **Chặn (401)** | **Cho phép (200)**<br>*(Chỉ thấy đơn của mình)* | **Cho phép (200)**<br>*(Thấy toàn bộ đơn)* | **Cho phép (200)**<br>*(Thấy toàn bộ đơn)* | **KB1:** Bắt buộc JWT Bearer + Phân tách phạm vi dữ liệu (Data Isolation) |
| 5 | `GET /api/orders/{id}` *(Đơn chính chủ)* | **Chặn (401)** | **Cho phép (200)** | **Cho phép (200)** | **Cho phép (200)** | **KB3:** Kiểm tra chủ sở hữu (`order.UserId == Claims.UserId`) |
| 6 | `GET /api/orders/{id}` *(Đơn người khác)* | **Chặn (401)** | **Chặn (403)**<br>*(Tuyệt đối không lộ tin)* | **Cho phép (200)** | **Cho phép (200)** | **KB3:** Chặn BOLA/IDOR khi xem + Ghi Audit Log vi phạm |
| 7 | `GET /api/orders/search` *(Từ khóa hợp lệ)* | **Chặn (401)** | **Cho phép (200)** | **Cho phép (200)** | **Cho phép (200)** | **KB4:** Input Validation + Parameterized Query EF Core LINQ |
| 8 | `GET /api/orders/search` *(Chuỗi SQLi)* | **Chặn (401)** | **Chặn (400)**<br>*(Không bao giờ lỗi 500)* | **Chặn (400)** | **Chặn (400)** | **KB4:** Regex Whitelist + Blacklist SQLi Pattern + Ghi Audit Log |
| 9 | `POST /api/orders` *(Tạo đơn mới)* | **Chặn (401)** | **Cho phép (201)**<br>*(Chặn 400 nếu tiền <= 0)* | **Cho phép (201)** | **Cho phép (201)** | **KB6:** Chống Mass Assignment (lấy `UserId` từ JWT) + Chặn số tiền âm |
| 10 | `PUT /api/orders/{id}` *(Cập nhật đơn)* | **Chặn (401)** | **Chính chủ (200)**<br>*(Chặn 403 nếu đơn người khác hoặc tự duyệt Completed)* | **Cho phép (200)** | **Cho phép (200)** | **KB6:** Chống IDOR khi Sửa + Phân quyền chuyển trạng thái `Completed` |
| 11 | `DELETE /api/orders/{id}` *(Xóa đơn)* | **Chặn (401)** | **Chính chủ (200)**<br>*(Chặn 403 nếu đơn người khác)* | **Chặn (403)** | **Cho phép (200)** | **KB6:** Chống IDOR khi Xóa đơn hàng |
| 12 | `POST /api/orders/{id}/upload` *(Đính kèm)* | **Chặn (401)** | **Chính chủ (200)**<br>*(Chặn 400/415 nếu giả mạo)* | **Chặn (403)** | **Cho phép (200)** | **KB7:** Kiểm tra chủ đơn + Chống Path Traversal/Double Ext + **Magic Bytes** |
| 13 | `GET /api/orders/rate-test` | **Chặn (401)** | **Cho phép (200)**<br>*(Chặn 429 nếu > 5 req/5s)* | **Cho phép (200)** | **Cho phép (200)** | **KB8:** Rate Limiting chống Spam API (`429 Too Many Requests` + `Retry-After`) |
| 14 | `GET /api/admin/users` | **Chặn (401)** | **Chặn (403)** | **Chặn (403)** | **Cho phép (200)** | **KB2:** RBAC Policy `AdminOnly` + Ẩn mật khẩu băm BCrypt |
| 15 | `GET /api/admin/audit-logs` | **Chặn (401)** | **Chặn (403)** | **Chặn (403)** | **Cho phép (200)** | **KB2:** RBAC Policy `AdminOnly` + Giám sát nhật ký kiểm toán an ninh |

---

## 3. BẢNG ĐỐI CHIẾU KẾT QUẢ THỰC NGHIỆM BEFORE / AFTER

### 3.1 Nhóm 4 kịch bản bảo mật cốt lõi (Theo đề cương đã phê duyệt: KB1 – KB4)

| Tiêu chí đánh giá | Kịch bản kiểm thử | Phiên bản V1 (Baseline - Chưa bảo mật) | Phiên bản V2 (Hardened - Đã bảo mật) | Kết luận & Ý nghĩa an toàn thông tin |
| :--- | :--- | :--- | :--- | :--- |
| **TC1: Kiểm soát xác thực (KB1)** | Gọi `GET /api/orders` khi không truyền Token | Trả về **`200 OK`**, rò rỉ toàn bộ danh sách đơn hàng cho người lạ. *(Broken Authentication)* | Trả về **`401 Unauthorized`**, chặn đứng yêu cầu và ghi log xác thực thất bại. | **Đạt tiêu chuẩn TC1:** Ngăn chặn tuyệt đối truy cập nặc danh vào tài nguyên nội bộ. |
| **TC2: Kiểm soát vai trò quản trị (KB2)** | `userA` hoặc `staff1` gọi API `GET /api/admin/users` | Trả về **`200 OK`**, lộ toàn bộ danh sách user và mật khẩu plaintext. *(Broken Function Level Authorization)* | Trả về **`403 Forbidden`**, từ chối truy cập và ghi nhận vi phạm phân quyền vào `AuditLogs`. | **Đạt tiêu chuẩn TC2:** Triển khai triệt để nguyên tắc đặc quyền tối thiểu (Least Privilege). |
| **TC3: Kiểm soát đối tượng BOLA/IDOR (KB3)** | `userA` gọi `GET /api/orders/3` (đơn hàng của `userB`) | Trả về **`200 OK`**, hiển thị đầy đủ thông tin đơn hàng và thông tin cá nhân của `userB`. | Trả về **`403 Forbidden`**, không để lộ bất kỳ thông tin nào của `userB` và ghi log cảnh báo IDOR. | **Đạt tiêu chuẩn TC3:** Ngăn chặn triệt để lỗ hổng nguy hiểm Top 1 OWASP API Security (BOLA). |
| **TC4: Kiểm soát đầu vào & SQLi (KB4)** | Gọi `GET /api/orders/search?keyword=' OR 1=1 --` | Trả về **`200 OK`**, dump toàn bộ dữ liệu hoặc trả về **`500`** làm lộ StackTrace hệ thống. | Trả về **`400 Bad Request`**, chặn đứng payload độc hại, bảo vệ CSDL nguyên vẹn. | **Đạt tiêu chuẩn TC4:** Loại trừ nguy cơ tấn công tiêm mã SQL Injection và từ chối dịch vụ. |

### 3.2 Nhóm 4 kịch bản phòng thủ chiều sâu mở rộng (Defense-in-Depth: KB5 – KB8 & OWASP Headers)

| Tiêu chí mở rộng | Kịch bản kiểm thử | Phiên bản V1 (Baseline - Chưa bảo mật) | Phiên bản V2 (Hardened - Đã bảo mật) | Kết luận & Ý nghĩa an toàn thông tin |
| :--- | :--- | :--- | :--- | :--- |
| **KB5: Thu hồi Token khi Đăng xuất (Token Revocation)** | Đăng xuất (`POST /api/auth/logout`) rồi dùng lại JWT cũ gọi `GET /api/auth/me` | Trả về **`200 OK`**, JWT cũ vẫn tiếp tục sử dụng được cho đến khi hết hạn tự nhiên. *(Lỗ hổng Token Replay)* | Trả về **`401 Unauthorized (TokenRevoked)`**, thu hồi ngay lập tức mã định danh `JTI` trong `TokenBlacklistService`. | Khắc phục điểm yếu kinh điển của Stateless JWT khi người dùng đăng xuất hoặc bị đánh cắp phiên. |
| **KB6: Logic nghiệp vụ, Mass Assignment & IDOR Sửa/Xóa** | Tạo đơn với số tiền âm `-999,999đ` (`POST /api/orders`) hoặc `userA` xóa đơn `#3` của `userB` (`DELETE /api/orders/3`) | Trả về **`200 OK`**, cho phép tạo đơn tiền âm, gán đơn cho người khác và xóa trái phép đơn của `userB`. | Trả về **`400 Bad Request`** với tiền âm; tự động lấy `UserId` từ JWT; chặn **`403 Forbidden`** khi Sửa/Xóa đơn người khác. | Bảo vệ toàn vẹn quy trình nghiệp vụ thương mại điện tử và chống thao túng dữ liệu (BOPLA). |
| **KB7: Kiểm tra Chữ ký Byte tệp tải lên (Magic Bytes)** | Tải lên file `webshell.jpg.aspx` hoặc file văn bản đổi đuôi `.jpg` vào `POST /api/orders/1/upload` | Trả về **`200 OK`**, lưu trực tiếp file độc hại lên máy chủ mà không kiểm tra nội dung thực tế. | Trả về **`400 Bad Request` / `415 Unsupported Media Type`**, đối chiếu chữ ký byte (`FF D8 FF`, `89 50 4E 47`, `%PDF`). | Ngăn chặn triệt để nguy cơ tải lên Webshell thực thi mã từ xa (RCE) và tấn công Path Traversal. |
| **KB8: Chống Brute-Force & Rate Limiting API** | Gửi liên tiếp > 5 requests trong 5 giây vào `GET /api/orders/rate-test` hoặc đăng nhập sai > 5 lần | Trả về **`200 OK`** không giới hạn, cho phép vét cạn mật khẩu và spam làm cạn kiệt tài nguyên. | Trả về **`429 Too Many Requests`** kèm Header `Retry-After: 5`, tạm khóa truy cập và ghi nhận vào `AuditLogs`. | Đảm bảo tính sẵn sàng (Availability) và phòng chống tấn công tự động hóa (OWASP API4:2023). |
| **TC5: OWASP Security Headers & Audit Logs** | Kiểm tra HTTP Response Headers & truy vấn `GET /api/admin/audit-logs` | Thiếu các HTTP Security Headers; không lưu trữ vết tấn công vào CSDL. | Tự động đính kèm 5 Header an ninh chuẩn OWASP; lưu đầy đủ 100% sự kiện vào bảng `AuditLogs`. | Đạt chuẩn quét tự động OWASP ZAP và đáp ứng yêu cầu điều tra số (Digital Forensics). |

---

## 4. HƯỚNG DẪN TỪNG BƯỚC CHẠY THỰC NGHIỆM ĐỐI CHỨNG

### Bước 1: Khởi động 2 phiên bản song song
Chạy script `.\run_all.ps1` hoặc mở 2 cửa sổ Terminal tại thư mục `ĐACN`:
- **Bản V1:** `http://localhost:5001` (Swagger UI: `http://localhost:5001/swagger`)
- **Bản V2:** `http://localhost:5002` (Swagger UI: `http://localhost:5002/swagger`)

### Bước 2: Kiểm thử tự động 17 bước bằng PowerShell
Chạy lệnh:
```powershell
powershell -ExecutionPolicy Bypass -File .\test_api.ps1
```
Script sẽ tự động kiểm tra toàn bộ 17 tiêu chí từ `KB1` đến `KB8`, kiểm tra `OWASP HTTP Security Headers` và truy vấn bảng `AuditLogs`.

### Bước 3: Kiểm chứng trực tiếp trên giao diện Web Cyber Security Dashboard
Mở trình duyệt truy cập `http://localhost:5001` và `http://localhost:5002`:
1. Nhấn **Chạy tất cả 4 kịch bản (Run All KB1–KB4)** ở Hàng 1 để đối chiếu 4 kịch bản cốt lõi theo đề cương.
2. Nhấn **Chạy tất cả kịch bản mở rộng (KB5–KB8)** ở Hàng 2 để đối chiếu 4 cơ chế phòng thủ chiều sâu.
3. Chuyển qua các Tab **Quản lý đơn hàng**, **Tạo đơn hàng mới**, **Quản trị người dùng** và **HTTP Security Headers (OWASP ZAP)** để kiểm thử tương tác trực tiếp.

---

## 5. HƯỚNG DẪN QUÉT LỖ HỔNG BẰNG CÔNG CỤ OWASP ZAP (ZAP SCANNING)

### 5.1 Quy trình quét tự động (Automated Scan)
1. Mở **OWASP ZAP** -> Chọn **Automated Scan**.
2. **Quét bản V1 (`http://localhost:5001/swagger/v1/swagger.json`):**
   - ZAP phát hiện các cảnh báo: *Missing Anti-Clickjacking Header (`X-Frame-Options`)*, *X-Content-Type-Options Header Missing*, *Content Security Policy (CSP) Header Not Set*, *SQL Injection / Server Error 500*.
3. **Quét bản V2 (`http://localhost:5002/swagger/v2/swagger.json`):**
   - Nhờ `SecurityAuditMiddleware` tự động gắn đầy đủ bộ 5 **OWASP HTTP Security Headers** (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `X-XSS-Protection: 1; mode=block`, `Referrer-Policy`, `Content-Security-Policy`) cùng cơ chế lọc đầu vào và xác thực JWT, các cảnh báo lỗ hổng trên bản V2 được khắc phục triệt để.

---

### KẾT LUẬN THỰC NGHIỆM
Hệ thống thực nghiệm đã chứng minh một cách trực quan, chặt chẽ và thuyết phục rằng việc kết hợp **4 lớp bảo mật cốt lõi (`KB1–KB4`)** theo đề cương nghiên cứu cùng **4 cơ chế phòng thủ chiều sâu (`KB5–KB8` + OWASP Security Headers + Audit Logging)** đã xây dựng được một kiến trúc bảo mật RESTful API toàn diện, đáp ứng xuất sắc các tiêu chuẩn của **OWASP API Security Top 10** và mục tiêu đề tài Đồ án chuyên ngành An toàn thông tin.
