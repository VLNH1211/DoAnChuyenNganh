# WebApi.V1_Vulnerable — Phiên Bản Thử Nghiệm Baseline (Red Team)

> **Dự án thành phần thuộc Đồ án Chuyên ngành An toàn thông tin (HUTECH)**  
> **Sinh viên:** Võ Lê Nhật Hoàng | **MSSV:** 2387700017 | **Lớp:** 23DATA1  
> **Nền tảng:** C# ASP.NET Core 8.0 LTS Web API | **Cổng dịch vụ:** `http://localhost:5001`  
> **Cơ sở dữ liệu:** SQL Server LocalDB (`SecurityLab_V1`)

---

## 1. MỤC ĐÍCH & Ý NGHĨA KHOA HỌC

Trong phương pháp nghiên cứu thực nghiệm đối chứng (**Before / After Testing Model**), phiên bản **`WebApi.V1_Vulnerable`** đóng vai trò là **mẫu đối chứng gốc (Baseline / Red Team)**. Dự án mô phỏng một hệ thống backend RESTful API điển hình viết bằng ASP.NET Core nhưng **cố ý thiếu vắng hoặc cấu hình sai các cơ chế bảo mật tiêu chuẩn**, dẫn đến các lỗ hổng nghiêm trọng nằm trong danh mục **OWASP Top 10 API Security Risks**.

Mục đích chính của phiên bản này:
1. Chứng minh các rủi ro an ninh xảy ra trên thực tế khi lập trình viên không khai thác đúng các tính năng bảo mật sẵn có của nền tảng ASP.NET Core.
2. Cung cấp môi trường mục tiêu để tiến hành kiểm thử thâm nhập (Penetration Testing) và ghi nhận dữ liệu đối chứng (mã HTTP `200 OK`, rò rỉ dữ liệu).

---

## 2. GIAO DIỆN CYBER DASHBOARD & 1-CLICK ATTACK SIMULATOR (V1)

Ứng dụng tích hợp sẵn giao diện web quản trị Cyber Security Dashboard tại `http://localhost:5001` với tông màu **Đỏ Cảnh Báo (Red Alert)**:

![Giao diện Bảng điều khiển Bản V1](../screenshots/01_V1_Dashboard_Overview.png)
*Hình 1: Giao diện tổng quan Web V1 tại cổng 5001 với tông đỏ cảnh báo và bộ 1-Click Attack Simulator.*

---

## 3. DANH SÁCH 8 LỖ HỔNG THỰC NGHIỆM ĐỐI CHỨNG (KB1 – KB8)

Toàn bộ các lỗ hổng đều được lập trình trực tiếp trong các Controller của ASP.NET Core để phục vụ thực nghiệm:

### 3.1 Nhóm 4 kịch bản cốt lõi (Khớp 100% Đề cương đã nộp)

| Kịch bản | Tên lỗ hổng & Endpoint | Cơ chế bị thiếu trên ASP.NET Core | Minh chứng hình ảnh |
| :---: | :--- | :--- | :---: |
| **KB1** | **Broken Authentication**<br>`GET /api/orders` | Endpoint không khai báo thuộc tính `[Authorize]`. Bất kỳ ai không có Token vẫn truy xuất được toàn bộ đơn hàng (`200 OK`). | ![KB1 Before](../screenshots/03_KB1_Before_V1_Unauthenticated_200.png) |
| **KB2** | **Broken Function Level Authorization (RBAC)**<br>`GET /api/admin/users` | Không khai báo `[Authorize(Roles = "Admin")]`. Khách hàng thường `userA` vẫn xem được danh sách Admin và **mật khẩu Plaintext** (`200 OK`). | ![KB2 Before](../screenshots/06_KB2_Before_V1_BrokenRBAC_200.png) |
| **KB3** | **Broken Object Level Authorization (BOLA / IDOR)**<br>`GET /api/orders/{id}` | Trong Action `GetOrderById`, không đối chiếu `order.UserId` với `User.Claims`. `userA` đổi `id=3` xem trộm toàn bộ đơn của `userB` (`200 OK`). | ![KB3 Before](../screenshots/09_KB3_Before_V1_IDOR_Leaked_200.png) |
| **KB4** | **SQL Injection & Data Leakage**<br>`GET /api/orders/search?keyword=...` | Sử dụng ghép chuỗi SQL thô nối trực tiếp tham số vào câu lệnh. Chuỗi `' OR 1=1 --` làm sai lệch mệnh đề `WHERE`, đổ sạch toàn bộ CSDL (`200 OK`). | ![KB4 Before](../screenshots/11_KB4_Before_V1_SQLi_Dumped_200.png) |

---

### 3.2 Nhóm 4 kịch bản phòng thủ chiều sâu mở rộng (Defense-in-Depth)

| Kịch bản | Tên lỗ hổng & Endpoint | Phân tích rủi ro an ninh trên V1 |
| :---: | :--- | :--- |
| **KB5** | **Lack of Token Revocation (Token Replay)**<br>`POST /api/auth/logout` -> `GET /api/auth/me` | Khi Logout, máy chủ chỉ trả thông báo mà không thu hồi Token. Kẻ tấn công đánh cắp JWT cũ vẫn tiếp tục sử dụng để gọi API bình thường (`200 OK`). |
| **KB6** | **Business Logic Flaw & Mass Assignment & IDOR Xóa**<br>`POST /api/orders` \| `DELETE /api/orders/{id}` | - Cho phép tạo đơn với số tiền âm `-999,999đ` (`200 OK`).<br>- Client tự gán `UserId` của người khác hoặc gán `Status="Completed"`.<br>- `userA` gửi lệnh xóa trái phép đơn hàng #3 của `userB`. |
| **KB7** | **Insecure File Upload & Missing Magic-Bytes Check**<br>`POST /api/orders/{id}/upload` | Chỉ kiểm tra phần mở rộng tệp đơn giản hoặc tin cậy `Content-Type`. Cho phép tải lên tệp nguy hiểm như `webshell.jpg.aspx` hoặc script giả dạng ảnh (`200 OK`). |
| **KB8** | **Lack of Resources & Rate Limiting (Spam / Brute-Force)**<br>`POST /api/auth/login` \| `GET /api/orders/rate-test` | Không áp dụng cơ chế giới hạn tần suất (Rate Limiting). Kẻ tấn công có thể spam liên tục hàng ngàn request mà không hề bị chặn, gây cạn kiệt tài nguyên máy chủ. |

---

### 3.3 Minh chứng kiểm tra liên hoàn toàn bộ kịch bản trên Bản V1

![Run All Kịch bản cốt lõi V1](../screenshots/13a_V1_RunAll.png)
*Hình 2: Chạy liên hoàn 4 kịch bản cốt lõi KB1–KB4 trên Bản V1 — Cả 4 kịch bản đều chuyển màu ĐỎ báo động lỗ hổng.*

![Run All Kịch bản mở rộng V1](../screenshots/15a_V1_RunAll_Extended_KB5_KB8.png)
*Hình 3: Chạy liên hoàn 4 kịch bản mở rộng KB5–KB8 trên Bản V1 — Cả 4 kịch bản đều bị khai thác thành công (HTTP 200 OK).*

---

## 4. HƯỚNG DẪN KHỞI ĐỘNG & KIỂM THỬ BẢN V1

### Khởi động độc lập
Tại thư mục gốc `ĐACN`, mở terminal PowerShell và chạy:
```powershell
dotnet run --project WebApi.V1_Vulnerable/WebApi.V1_Vulnerable.csproj --urls=http://localhost:5001
```

### Các đường dẫn kiểm thử
- **Giao diện Dashboard V1:** `http://localhost:5001`
- **Swagger UI:** `http://localhost:5001/swagger`
- **Tài khoản kiểm thử:**
  - `userA` / `UserA@123` (Người dùng thường - Chủ sở hữu đơn #1, #2, #5)
  - `userB` / `UserB@123` (Người dùng thường - Chủ sở hữu đơn #3, #4)
  - `staff1` / `Staff@123` (Nhân viên)
  - `admin` / `Admin@123` (Quản trị viên)
