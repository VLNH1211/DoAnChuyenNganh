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

Hệ thống được thiết kế theo mô hình thực nghiệm đối chứng (**Before/After Testing Model**) với hai phiên bản độc lập trong cùng một Solution:

```
                                  +---------------------------------------------+
                                  |            Postman & OWASP ZAP              |
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
                  Cố ý thiếu các cơ chế bảo mật                    Áp dụng 4 lớp kiểm soát bảo mật:
                  - Không kiểm tra JWT ở KB1                      - Bắt buộc JWT Bearer (KB1)
                  - Không có Policy Admin ở KB2                   - RBAC Policy AdminOnly (KB2)
                  - Không kiểm tra chủ đơn ở KB3                  - ABAC / Chặn BOLA-IDOR (KB3)
                  - Ghép chuỗi SQL thô ở KB4                      - Validation + Parameterized (KB4)
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
- **Cơ chế xác thực & băm:** JWT Bearer Token (HMAC-SHA256, RFC 7519), BCrypt Password Hashing (`BCrypt.Net-Next`)
- **Tài liệu hóa API:** Swagger / OpenAPI UI với cấu hình nút `Authorize` Bearer Token
- **Giám sát & Nhật ký:** Bảng `AuditLogs` (tự động ghi nhận mã 401, 403, 400, 200 kèm Username, Role, IP)

### 1.2 Danh sách tài khoản mẫu (Seed Data)
| Tài khoản (Username) | Mật khẩu mẫu | Vai trò (Role) | Mô tả & Quyền sở hữu dữ liệu |
| :--- | :--- | :--- | :--- |
| `admin` | `Admin@123` | `Admin` | Quản trị viên toàn quyền hệ thống, xem toàn bộ người dùng, đơn hàng và Audit Log. |
| `staff1` | `Staff@123` | `NhanVien` | Nhân viên vận hành; được xem toàn bộ đơn hàng nhưng bị cấm truy cập API Quản trị. |
| `userA` | `UserA@123` | `NguoiDung` | Khách hàng thông thường (Võ Lê Nhật Hoàng); chủ sở hữu các đơn hàng: **#1, #2, #5**. |
| `userB` | `UserB@123` | `NguoiDung` | Khách hàng thông thường (Trần Thị Bích); chủ sở hữu các đơn hàng: **#3, #4**. |

---

## 2. MA TRẬN PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP (ACCESS CONTROL MATRIX)

Ma trận dưới đây mô tả chính sách phân quyền áp dụng trên bản an toàn **`WebApi.V2_Secure`**:

| STT | Phương thức & Endpoint | Chưa đăng nhập (Anonymous) | Người dùng (`userA` / `userB`) | Nhân viên (`staff1`) | Quản trị viên (`admin`) | Cơ chế kiểm soát bảo mật |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- |
| 1 | `POST /api/auth/login` | **Cho phép (200)** | **Cho phép (200)** | **Cho phép (200)** | **Cho phép (200)** | Xác thực BCrypt + Cấp JWT Token |
| 2 | `GET /api/orders` | **Chặn (401)** | **Cho phép (200)**<br>*(Chỉ thấy đơn của mình)* | **Cho phép (200)**<br>*(Thấy toàn bộ đơn)* | **Cho phép (200)**<br>*(Thấy toàn bộ đơn)* | **KB1:** Bắt buộc JWT Bearer + Phân định phạm vi dữ liệu |
| 3 | `GET /api/orders/{id}` *(Đơn của chính mình)* | **Chặn (401)** | **Cho phép (200)** | **Cho phép (200)** | **Cho phép (200)** | **KB3:** Kiểm tra chủ sở hữu (`UserId == Claims.UserId`) |
| 4 | `GET /api/orders/{id}` *(Đơn của người khác)* | **Chặn (401)** | **Chặn (403)**<br>*(Tuyệt đối không lộ tin)* | **Cho phép (200)** | **Cho phép (200)** | **KB3:** Chặn BOLA/IDOR + Ghi Audit Log vi phạm |
| 5 | `GET /api/orders/search` *(Đầu vào hợp lệ)* | **Chặn (401)** | **Cho phép (200)** | **Cho phép (200)** | **Cho phép (200)** | **KB4:** Parameterized Query EF Core |
| 6 | `GET /api/orders/search` *(Đầu vào SQLi)* | **Chặn (401)** | **Chặn (400)**<br>*(Báo lỗi, không lỗi 500)* | **Chặn (400)** | **Chặn (400)** | **KB4:** Input Regex Validation + Blacklist Pattern |
| 7 | `GET /api/admin/users` | **Chặn (401)** | **Chặn (403)** | **Chặn (403)** | **Cho phép (200)** | **KB2:** RBAC Policy `AdminOnly` |
| 8 | `GET /api/admin/audit-logs` | **Chặn (401)** | **Chặn (403)** | **Chặn (403)** | **Cho phép (200)** | **KB2:** RBAC Policy `AdminOnly` + Giám sát an ninh |

---

## 3. BẢNG ĐỐI CHIẾU KẾT QUẢ THỰC NGHIỆM BEFORE / AFTER (TC1 – TC5)

| Tiêu chí đánh giá | Kịch bản kiểm thử | Phiên bản V1 (Baseline - Chưa bảo mật) | Phiên bản V2 (Hardened - Đã bảo mật) | Kết luận & Ý nghĩa an toàn thông tin |
| :--- | :--- | :--- | :--- | :--- |
| **TC1: Kiểm soát xác thực (KB1)** | Gọi `GET /api/orders` khi không truyền Token | Trả về **`200 OK`**, rò rỉ toàn bộ danh sách đơn hàng cho người lạ. *(Lỗ hổng Broken Authentication)* | Trả về **`401 Unauthorized`**, chặn đứng yêu cầu và ghi log xác thực thất bại. | **Đạt tiêu chuẩn TC1:** Ngăn chặn tuyệt đối truy cập nặc danh vào tài nguyên nội bộ. |
| **TC2: Kiểm soát vai trò quản trị (KB2)** | `userA` hoặc `staff1` gọi API `GET /api/admin/users` | Trả về **`200 OK`**, lộ toàn bộ danh sách user và mật khẩu plaintext. *(Lỗ hổng Broken Function Level Authorization)* | Trả về **`403 Forbidden`**, từ chối truy cập và ghi nhận vi phạm phân quyền vào `AuditLogs`. | **Đạt tiêu chuẩn TC2:** Triển khai triệt để nguyên tắc đặc quyền tối thiểu (Least Privilege). |
| **TC3: Kiểm soát đối tượng BOLA/IDOR (KB3)** | `userA` gọi `GET /api/orders/3` (đơn hàng của `userB`) | Trả về **`200 OK`**, hiển thị đầy đủ thông tin đơn hàng và thông tin cá nhân của `userB`. | Trả về **`403 Forbidden`**, không để lộ bất kỳ thông tin nào của `userB` và ghi log cảnh báo IDOR. | **Đạt tiêu chuẩn TC3:** Ngăn chặn triệt để lỗ hổng nguy hiểm Top 1 OWASP API Security (BOLA). |
| **TC4: Kiểm soát đầu vào & SQLi (KB4)** | Gọi `GET /api/orders/search?keyword=' OR 1=1 --` | Trả về **`200 OK`**, dump toàn bộ dữ liệu hoặc trả về **`500`** làm lộ StackTrace hệ thống. | Trả về **`400 Bad Request`**, chặn đứng payload độc hại, bảo vệ CSDL nguyên vẹn. | **Đạt tiêu chuẩn TC4:** Loại trừ nguy cơ tấn công tiêm mã SQL Injection và từ chối dịch vụ. |
| **TC5: Ghi vết & Giám sát (Audit Log)** | Theo dõi và tra cứu nhật ký kiểm toán hệ thống | Không có cơ chế ghi nhận hoặc ghi nhận sơ sài, không lưu trữ vết tấn công. | Tự động ghi lại toàn bộ các sự kiện 401, 403, 400 vào bảng `AuditLogs` và Console theo thời gian thực. | **Đạt tiêu chuẩn TC5:** Đáp ứng yêu cầu Incident Response và Forensic trong ATTT. |

---

## 4. HƯỚNG DẪN TỪNG BƯỚC CHẠY THỰC NGHIỆM ĐỐI CHỨNG

### Bước 1: Khởi động 2 phiên bản song song
Mở 2 cửa sổ Terminal (PowerShell) tại thư mục `ĐACN`:

- **Cửa sổ 1 (Chạy bản V1):**
  ```powershell
  dotnet run --project WebApi.V1_Vulnerable/WebApi.V1_Vulnerable.csproj
  ```
  *Ứng dụng V1 sẽ lắng nghe tại `http://localhost:5001`, Swagger UI tại `http://localhost:5001/swagger`.*

- **Cửa sổ 2 (Chạy bản V2):**
  ```powershell
  dotnet run --project WebApi.V2_Secure/WebApi.V2_Secure.csproj
  ```
  *Ứng dụng V2 sẽ lắng nghe tại `http://localhost:5002`, Swagger UI tại `http://localhost:5002/swagger`.*

### Bước 2: Kiểm thử tự động bằng Postman
1. Khởi động ứng dụng **Postman**.
2. Nhấn nút **Import** -> chọn file `postman/Postman_Collection_KB1_KB4.json`.
3. Chạy các request theo từng thư mục hoặc sử dụng **Postman Collection Runner**:
   - Thư mục `00. Lấy JWT Token`: Lần lượt chạy các request login để tự động lưu các biến `token_admin`, `token_staff1`, `token_userA`, `token_userB`.
   - Thư mục `01. KB1`: Kiểm chứng V1 trả về 200 (Lỗ hổng) và V2 trả về 401 (Chặn thành công).
   - Thư mục `02. KB2`: Kiểm chứng V1 trả về 200 và V2 trả về 403 khi `userA` hoặc `staff1` gọi API Admin.
   - Thư mục `03. KB3`: Kiểm chứng V1 trả về 200 (IDOR) và V2 trả về 403 khi `userA` xem đơn của `userB`.
   - Thư mục `04. KB4`: Kiểm chứng V1 bị SQLi và V2 chặn đứng với mã 400 Bad Request.
   - Thư mục `05. Giám sát`: Tra cứu các bản ghi Audit Log vừa phát sinh trên V2.
4. Mọi request đều có mã kiểm thử tự động (**Assertions PASS 100%**).

### Bước 3: Kiểm chứng trực tiếp trên SQL Server Management Studio (SSMS)
1. Mở SSMS, kết nối đến server: `(localdb)\MSSQLLocalDB`.
2. Mở file [init_and_verify.sql](file:///c:/Users/volen/OneDrive/Documents/DoAnChuyenNganh/ĐACN/sql/init_and_verify.sql).
3. Thực thi lần lượt các khối truy vấn kiểm chứng:
   - **Truy vấn 1:** Kiểm chứng chủ sở hữu đơn hàng (đơn #3, #4 thuộc về `userB`).
   - **Truy vấn 2:** Kiểm chứng dữ liệu vẫn nguyên vẹn sau tấn công SQL Injection.
   - **Truy vấn 3 & 4:** Truy vấn bảng `AuditLogs` trên `SecurityLab_V2` để xem thống kê các mã lỗi 401, 403, 400.

---

## 5. HƯỚNG DẪN QUÉT LỖ HỔNG BẰNG CÔNG CỤ OWASP ZAP (ZAP SCANNING)

OWASP Zed Attack Proxy (ZAP) là công cụ kiểm thử bảo mật chuyên dụng dùng để phát hiện lỗ hổng trên ứng dụng Web và API.

### 5.1 Quy trình quét tự động (Automated Scan)
1. Mở **OWASP ZAP**.
2. Tại màn hình chính chọn tab **Automated Scan**.
3. **Quét bản V1:**
   - Nhập URL to attack: `http://localhost:5001/swagger/v1/swagger.json` (hoặc `http://localhost:5001/api/orders`).
   - Nhấn **Attack**.
   - Quan sát tab **Alerts**: ZAP sẽ phát hiện các cảnh báo nghiêm trọng:
     - *Absence of Anti-CSRF Tokens / Missing Authorization*
     - *SQL Injection Potential* (khi spider quét qua endpoint `/api/orders/search`)
     - *Information Disclosure (Server Error / Stack Trace)* khi gửi ký tự đặc biệt.
4. **Quét bản V2:**
   - Nhập URL to attack: `http://localhost:5002/swagger/v2/swagger.json`.
   - Nhấn **Attack**.
   - Quan sát tab **Alerts**: Các lỗ hổng High/Medium liên quan đến SQL Injection, Unauthenticated Access và Information Disclosure biến mất hoàn toàn.

### 5.2 Quy trình quét nâng cao với JWT Token (Authenticated Scan trong ZAP)
Để ZAP kiểm thử các API yêu cầu đăng nhập trên bản V2:
1. Trong ZAP, vào menu **Tools** -> **Options** -> **Replacer**.
2. Nhấn **Add** để tạo quy tắc thêm Header:
   - **Description:** `Inject JWT Token for V2`
   - **Match Type:** `Request Header (add if not present)`
   - **Match String:** `Authorization`
   - **Replacement String:** `Bearer <dán_token_userA_tại_đây>`
   - Đánh dấu chọn **Enable**.
3. Vào tab **Manual Explore** -> nhập `http://localhost:5002/api/orders` -> nhấn **Launch Browser** hoặc **Explore**.
4. ZAP sẽ gửi yêu cầu kèm Header Authorization, xác nhận API phản hồi `200 OK` an toàn mà không phát sinh bất kỳ lỗi bảo mật nghiêm trọng nào.

---

### KẾT LUẬN THỰC NGHIỆM
Hệ thống thực nghiệm đã chứng minh một cách trực quan, chặt chẽ và thuyết phục rằng:
- Việc áp dụng kết hợp **JWT Authentication**, **Role-Based Access Control (RBAC)**, **Object-Level Access Control (ABAC/IDOR defense)**, **Input Validation & Parameterized Queries**, cùng với **Security Audit Logging** đã loại bỏ hoàn toàn 4 bề mặt tấn công phổ biến nhất hiện nay trên các hệ thống Web API, đáp ứng trọn vẹn mục tiêu đề ra trong đề tài đồ án chuyên ngành.
