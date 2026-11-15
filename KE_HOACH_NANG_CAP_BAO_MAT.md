# BÁO CÁO KẾ HOẠCH NÂNG CẤP & KIỂM THỬ CƠ CHẾ PHÒNG THỦ TOÀN DIỆN CHO HỆ THỐNG RESTFUL API (ASP.NET CORE 8.0)

> **Đề tài:** Xây dựng và kiểm thử cơ chế phòng thủ cho hệ thống RESTful API trên nền tảng ASP.NET Core  
> **Sinh viên thực hiện:** Võ Lê Nhật Hoàng  
> **Mã số sinh viên (MSSV):** 2387700017 — **Lớp:** 23DATA1  
> **Dự án áp dụng:** `ĐACN\EcommerceSecurityApi` (.NET 8.0 Web API)  
> **Dự án kiểm thử:** `ĐACN\EcommerceSecurityApi.Tests` (xUnit + WebApplicationFactory)  
> **Cơ sở dữ liệu:** Microsoft SQL Server LocalDB (`EcommerceSecurityDb`)  
> **Tiêu chuẩn an ninh đối chiếu:** OWASP API Security Top 10 (2023)

---

## 1. TỔNG QUAN MỤC TIÊU & NGUYÊN TẮC BẤT BIẾN

### 1.1. Mục tiêu nâng cấp
1. **Hoàn thiện cơ chế phòng thủ đa tầng:** Mở rộng và chuẩn hóa toàn bộ các chốt chặn an toàn thông tin của hệ thống API **TechSec Gear** bằng các tính năng chuẩn hóa (Native/First-party) của **ASP.NET Core 8.0**, bao phủ đầy đủ khung tiêu chuẩn quốc tế **OWASP API Security Top 10 (2023)**.
2. **Khép kín chu trình Giám sát An ninh (Active SOC Defense):** Tự động phát hiện và ghi nhận mọi hành vi vi phạm (từ dò quét Brute-force, leo quyền BFLA, đọc trộm BOLA đến tấn công làm tràn tài nguyên) vào bảng `SecurityLogs` theo thời gian thực để hiển thị trên SOC Dashboard.
3. **Hiện thực hóa vế "Kiểm thử" của Đề tài:** Xây dựng bộ kiểm thử an ninh tự động độc lập bằng **xUnit** kết hợp **WebApplicationFactory** và **InMemory Database**, cho phép kiểm chứng tự động toàn bộ 8 kịch bản tấn công an ninh trọng tâm mà không làm ảnh hưởng đến cơ sở dữ liệu thực nghiệm.

### 1.2. Ba nguyên tắc bất biến (Đã tuân thủ 100%)
* ✅ **Bảo toàn nguyên vẹn CSDL:** Tuyệt đối không thay đổi schema, không thêm migration làm rách cấu trúc bảng, giữ nguyên toàn bộ tài khoản thực nghiệm (`admin`, `nhanvien`, `vlnh`, `vlnh1`) và dữ liệu nghiệp vụ trên `EcommerceSecurityDb`.
* ✅ **Kế thừa và không phá vỡ tính năng cũ:** Các luồng JWT Bearer, mã hóa BCrypt, RBAC, BOLA/IDOR tại `OrdersController`, `ActiveUserMiddleware`, cơ chế chống Price Tampering và Giao diện Dashboard SOC vẫn hoạt động mượt mà 100%.
* ✅ **Đạt độ tin cậy tuyệt đối:** Biên dịch Solution đạt `0 Warning(s), 0 Error(s)` và chạy kiểm thử tự động `dotnet test` đạt **8/8 Test Cases PASSED (100%)**.

---

## 2. BẢN ĐỒ ĐỐI CHIẾU CHUẨN OWASP API SECURITY TOP 10 (2023)

| Mã OWASP | Tên Lỗ hổng & Rủi ro An ninh | Cơ chế phòng thủ trong hệ thống TechSec Gear API | Tình trạng & Phương thức kiểm chứng |
| :--- | :--- | :--- | :--- |
| **API1:2023** | **Broken Object Level Authorization (BOLA/IDOR)** | Kiểm tra nghiêm ngặt quyền sở hữu `order.UserId == currentUserId` hoặc quyền Admin/Staff tại `OrdersController.cs`. Từ chối truy cập trái phép bằng `HTTP 403` kèm tự động ghi log SOC `BOLA_IDOR_VIOLATION`. | ✅ **Đã hoàn thiện & Pass Test 1** |
| **API2:2023** | **Broken Authentication** | Xác thực JWT Bearer, băm mật khẩu bằng BCrypt (Salt Work Factor = 12), giới hạn thời hạn Token, thu hồi ngay lập tức qua API `POST /api/auth/logout` lưu JTI vào Memory Cache Blacklist. | ✅ **Đã hoàn thiện & Pass Test 6** |
| **API3:2023** | **Broken Object Property Level Authorization** | Tách bạch DTO đầu vào (`CreateOrderDto`, `CreateOrderItemRequestDto`), chống triệt để Mass Assignment. Server tự tính giá và tổng tiền từ giá niêm yết trong DB, phớt lờ hoàn toàn giá Client gửi lên. | ✅ **Đã hoàn thiện & Pass Test 4** |
| **API4:2023** | **Unrestricted Resource Consumption** | Tích hợp ASP.NET Core 8 Rate Limiter: Policy `LoginLimit` (5 req/phút), Policy `OrderLimit` (10 req/phút), `GlobalLimiter` (100 req/phút). Trả `HTTP 429 Too Many Requests` và ghi log SOC `RATE_LIMIT_EXCEEDED`. | ✅ **Đã hoàn thiện & Pass Test 7** |
| **API5:2023** | **Broken Function Level Authorization (BFLA)** | Phân quyền vai trò chi tiết `[Authorize(Roles = "...")]`. Tự động bắt lỗi leo quyền qua `JwtBearerEvents.OnForbidden`, trả `HTTP 403` (`BFLA_FORBIDDEN`) và ghi log SOC `BFLA_PRIVILEGE_ESCALATION`. | ✅ **Đã hoàn thiện & Pass Test 2, 3** |
| **API6:2023** | **Unrestricted Access to Sensitive Business Flows** | Chống lạm dụng luồng nghiệp vụ: Gom nhóm sản phẩm trùng `ProductId` (`GroupBy`), bọc giao dịch `BeginTransactionAsync()`, kiểm tra nghiêm ngặt State Machine trạng thái đơn hàng (`Pending` -> `Processing` -> `Completed`/`Cancelled`). | ✅ **Đã hoàn thiện & Pass Test 4** |
| **API7:2023** | **Server Side Request Forgery (SSRF)** | Hệ thống không cung cấp API fetch URL tự do từ phía client, toàn bộ ảnh và tài nguyên tĩnh được phục vụ cục bộ tại `wwwroot`. | ✅ **Bảo đảm theo thiết kế hệ thống** |
| **API8:2023** | **Security Misconfiguration** | Tắt header `Server: Kestrel`, chèn bộ Security Headers phòng thủ (`nosniff`, `DENY`, `no-referrer`, `no-store`), triển khai `GlobalExceptionHandler` (`IExceptionHandler`) chống rò rỉ StackTrace/SQL, siết CORS. | ✅ **Đã hoàn thiện & Pass Test 8** |
| **API9/10** | **Improper Inventory / Testing** | Thiết lập Swagger UI chuẩn OpenApi có chú thích Bearer Token và xây dựng bộ kiểm thử an ninh tự động `EcommerceSecurityApi.Tests` (xUnit). | ✅ **Đã hoàn thiện & Pass 8/8 Tests** |

---

## 3. CHI TIẾT KỸ THUẬT CÁC GIAI ĐOẠN ĐÃ THỰC HIỆN

### 3.1. Phase 1 — Chống Brute-Force & Spam API bằng Rate Limiting (OWASP API4:2023)
* **Tạo mới:** `ĐACN\EcommerceSecurityApi\Helpers\ClientInfoHelper.cs`
  * Thuật toán trích xuất chính xác địa chỉ IP nguồn: Ưu tiên đọc `CF-Connecting-IP` (khi chạy Cloudflare Tunnel demo trực tuyến), kế đến là `X-Forwarded-For` (qua Reverse Proxy), cuối cùng mới fallback về `RemoteIpAddress`.
* **Cấu hình tại `Program.cs`:**
  * Sử dụng thư viện chuẩn `.NET 8` (`Microsoft.AspNetCore.RateLimiting`, `System.Threading.RateLimiting`).
  * **Policy `LoginLimit`:** Giới hạn tối đa **5 requests / 60 giây** cho endpoint `POST /api/auth/login` theo địa chỉ IP nguồn.
  * **Policy `OrderLimit`:** Giới hạn tối đa **10 requests / 60 giây** cho endpoint `POST /api/orders` theo `UserId` (nếu đã đăng nhập) hoặc IP nguồn.
  * **Policy `GlobalLimiter`:** Giới hạn rộng **100 requests / 60 giây** bảo vệ toàn bộ API hệ thống trước các công cụ quét tự động.
  * **Xử lý sự kiện `options.OnRejected`:**
    * Thiết lập `HTTP 429 Too Many Requests` và bổ sung header `Retry-After`.
    * Tự động gọi `ISecurityLogService.LogIncidentAsync` ghi cảnh báo `RATE_LIMIT_EXCEEDED` (`Critical` cho login, `Warning` cho các API khác) lên bảng `SecurityLogs`.
    * Trả về JSON chuẩn:
      ```json
      {
        "success": false,
        "statusCode": 429,
        "error": "RATE_LIMIT_EXCEEDED",
        "message": "🛡️ [PHÒNG THỦ RATE LIMIT KÍCH HOẠT]: Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau.",
        "retryAfterSeconds": 60,
        "timestamp": "2026-11-15T12:30:58Z"
      }
      ```
* **Chỉnh sửa Controllers:** Gắn thuộc tính `[EnableRateLimiting("LoginLimit")]` cho hàm `Login` trong `AuthController.cs` và `[EnableRateLimiting("OrderLimit")]` cho hàm `CreateOrder` trong `OrdersController.cs`.

---

### 3.2. Phase 2 — Tự Động Ghi Nhận BFLA Lên SOC & Bảo Vệ Luồng Đơn Hàng (OWASP API5 & API6:2023)
* **Tự động bắt lỗi leo quyền BFLA (`Program.cs`):**
  * Tích hợp sự kiện `JwtBearerEvents.OnForbidden` trong phương thức `AddJwtBearer`:
    * Bắt mọi trường hợp người dùng đã có token hợp lệ nhưng bị cơ chế `[Authorize(Roles = "...")]` từ chối truy cập (ví dụ: tài khoản `KhachHang` gọi API Admin hoặc tài khoản `NhanVien` gọi lệnh xóa sản phẩm).
    * Tự động trích xuất `userId`, `username`, `roles`, `method`, `path`, `ipAddress` và ghi vào CSDL với `incidentType = "BFLA_PRIVILEGE_ESCALATION"`, mức độ `Critical`.
    * Trả về phản hồi `HTTP 403 Forbidden` đồng nhất:
      ```json
      {
        "success": false,
        "statusCode": 403,
        "error": "BFLA_FORBIDDEN",
        "message": "🛡️ [PHÒNG THỦ BFLA KÍCH HOẠT]: Bạn không có đặc quyền truy cập tài nguyên này (Broken Function Level Authorization)!",
        "detail": "Tài khoản 'customer1' với vai trò [KhachHang] không được cấp phép thực thi hành động 'GET /api/auth/admin-only'. Hành vi đã được ghi lại trên Hệ thống Giám sát An ninh (SOC).",
        "timestamp": "2026-11-15T12:30:58Z"
      }
      ```
* **Gia cố luồng nghiệp vụ đơn hàng (`OrdersController.cs`):**
  * **Chống lách tồn kho:** Kiểm tra `Quantity > 0`, sau đó gộp nhóm các mặt hàng trùng mã sản phẩm bằng LINQ `GroupBy(i => i.ProductId)` để tính tổng số lượng thực sự yêu cầu trước khi so khớp tồn kho trong kho hàng.
  * **Đảm bảo tính toàn vẹn (ACID Transaction):** Bọc thao tác trừ tồn kho và tạo đơn hàng trong `BeginTransactionAsync()`. Bổ sung kiểm tra `_context.Database.IsRelational()` để tương thích hoàn hảo giữa môi trường SQL Server thật và môi trường InMemory Testing.
  * **Xây dựng State Machine chuyển trạng thái đơn hàng (`UpdateOrderStatus`):**
    * Chỉ cho phép chuyển đổi hợp lệ: `Pending` $\rightarrow$ `Processing` $\rightarrow$ `Completed` hoặc `Cancelled`.
    * Từ chối chuyển ngược hoặc can thiệp vào các đơn hàng đã ở trạng thái kết thúc (`Completed` hoặc `Cancelled`) bằng mã `HTTP 400 Bad Request` (`INVALID_STATUS_TRANSITION`).
    * Tự động hoàn trả tồn kho vào bảng `Products` nếu đơn hàng chuyển sang trạng thái `Cancelled`.

---

### 3.3. Phase 3 — Thu Hồi JWT Token Khi Đăng Xuất (Token Revocation - OWASP API2:2023)
* **Nguyên tắc kỹ thuật:** Tránh hoàn toàn việc sửa đổi bảng CSDL `Users` hay tạo bảng mới bằng cách áp dụng **In-Memory Token Blacklist** lưu trên RAM máy chủ thông qua `IMemoryCache` (Native .NET 8).
* **Tạo mới:**
  * `ĐACN\EcommerceSecurityApi\Services\ITokenBlacklistService.cs`
  * `ĐACN\EcommerceSecurityApi\Services\TokenBlacklistService.cs`
* **Cơ chế hoạt động:**
  1. Khi người dùng bấm Đăng xuất, client gửi `POST /api/auth/logout` kèm Bearer Token hiện tại.
  2. Server trích xuất claim `jti` (JWT Unique ID) và claim `exp` (thời điểm hết hạn) từ token.
  3. Đưa `jti` vào `IMemoryCache` với thời hạn sống (`AbsoluteExpiration`) bằng đúng thời điểm hết hạn `exp` của token $\rightarrow$ Tự động giải phóng RAM khi token tự nhiên hết hạn.
  4. Ghi nhận nhật ký an ninh `USER_LOGOUT` (`Info`) lên SOC Dashboard.
* **Chặn đứng tại `ActiveUserMiddleware.cs`:**
  * Kiểm tra `blacklistService.IsTokenRevoked(jti)` ngay từ tầng RAM trước khi thực hiện bất kỳ truy vấn CSDL nào.
  * Nếu Token đã bị thu hồi $\rightarrow$ ngắt pipeline ngay lập tức và trả về `HTTP 401 Unauthorized`:
    ```json
    {
      "success": false,
      "statusCode": 401,
      "error": "TOKEN_REVOKED",
      "message": "🛡️ [PHIÊN ĐĂNG NHẬP ĐÃ HẾT HIỆU LỰC]: Mã JWT Token này đã bị thu hồi do người dùng đã đăng xuất khỏi hệ thống. Vui lòng đăng nhập lại!",
      "timestamp": "2026-11-15T12:30:58Z"
    }
    ```
* **Tích hợp giao diện SPA (`wwwroot\index.html`):** Hàm `handleLogout()` gọi API `POST /api/auth/logout` trước khi xóa phiên khỏi `sessionStorage`.

---

### 3.4. Phase 4 — Hardening HTTP Pipeline & Security Headers (OWASP API8:2023)
* **Tắt Server Header của Web Server:**
  * Bổ sung cấu hình `builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false)` tại `Program.cs`. Triệt tiêu hoàn toàn header `Server: Kestrel` nhằm giấu thông tin kiến trúc máy chủ.
* **Tạo mới `SecurityHeadersMiddleware.cs`:**
  * Chèn tự động các HTTP Headers phòng ngự chuẩn hóa:
    * `X-Content-Type-Options: nosniff`: Ngăn chặn trình duyệt tự động đoán định kiểu nội dung, phòng chống mã độc núp bóng file ảnh/văn bản.
    * `X-Frame-Options: DENY`: Chống nhúng website vào thẻ iframe, vô hiệu hóa hoàn toàn nguy cơ Clickjacking.
    * `Referrer-Policy: no-referrer`: Không gửi thông tin URL nguồn khi điều hướng sang trang ngoài.
    * `Permissions-Policy: geolocation=(), microphone=(), camera=()`: Vô hiệu hóa các quyền truy cập phần cứng nhạy cảm.
    * `X-XSS-Protection: 1; mode=block`: Kích hoạt bộ lọc XSS trên các trình duyệt cũ.
    * `Cache-Control: no-store, no-cache, must-revalidate, max-age=0`: Ngăn trình duyệt và proxy trung gian lưu đệm dữ liệu của toàn bộ endpoint `/api/*`.
* **Tạo mới `GlobalExceptionHandler.cs` (Chuẩn `.NET 8 IExceptionHandler`):**
  * Đăng ký `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` và `app.UseExceptionHandler()`.
  * Gom bắt toàn bộ lỗi ngoại lệ chưa xử lý, ghi đầy đủ Stack Trace ở log server và chỉ trả mã `HTTP 500` an toàn ra client kèm mã định danh `traceId`, chống rò rỉ cấu trúc CSDL hoặc tên bảng SQL.
* **Siết chặt cấu hình CORS (`appsettings.json`):** Khai báo danh sách `AllowedOrigins` rõ ràng cho các môi trường chạy thử nghiệm.

---

### 3.5. Phase 5 — Xây Dựng Bộ Kiểm Thử An Ninh Tự Động (xUnit Test Suite)
* **Khởi tạo Project kiểm thử:** `EcommerceSecurityApi.Tests` (.NET 8.0) sử dụng bộ thư viện chính thống:
  * `Microsoft.AspNetCore.Mvc.Testing` (v8.0.11)
  * `Microsoft.EntityFrameworkCore.InMemory` (v8.0.11)
  * `Microsoft.NET.Test.Sdk` (v17.11.1)
  * `xunit` (v2.9.2) & `xunit.runner.visualstudio` (v2.8.2)
* **Lớp giả lập môi trường `CustomWebApplicationFactory.cs`:**
  * Sử dụng cơ chế In-Memory Database cô lập hoàn toàn (`TestSecurityDb_<GUID>`), tự động seed dữ liệu mẫu độc lập (`customer1`, `customer2`, `locked_user`, `staff_tester`, đơn hàng `#999`), bảo vệ an toàn 100% cho CSDL thật `EcommerceSecurityDb`.
* **Bộ test case bao phủ 8 kịch bản an ninh trọng tâm (`SecurityTests.cs`):**
  1. `Test1_BOLA_GetOtherUserOrder_Returns403_AndCreatesCriticalSecurityLog`: Khách hàng 1 cố đọc trộm đơn hàng của Khách hàng 2 $\rightarrow$ Bị chặn `403` và sinh log SOC `BOLA_IDOR_VIOLATION`.
  2. `Test2_BFLA_CustomerAccessAdminEndpoint_Returns403_AndCreatesSecurityLog`: Khách hàng gọi API Admin $\rightarrow$ Bị chặn `403` và sinh log SOC `BFLA_PRIVILEGE_ESCALATION`.
  3. `Test3_BFLA_StaffDeleteProduct_Returns403_AndCreatesSecurityLog`: Nhân viên gọi API xóa sản phẩm $\rightarrow$ Bị chặn `403` và sinh log SOC `BFLA_PRIVILEGE_ESCALATION`.
  4. `Test4_DataTampering_CreateOrder_UsesServerProductPrice_IgnoresTamperedInput`: Gửi request cố tình giả mạo giá tiền $\rightarrow$ Server tự tính đúng theo đơn giá CSDL.
  5. `Test5_AccountSuspension_LockedUserWithValidToken_BlockedImmediatelyWith403`: Tài khoản bị khóa trong DB gửi token hợp lệ $\rightarrow$ Middleware chặn ngay lập tức với `403 Forbidden` (`ACCOUNT_SUSPENDED`).
  6. `Test6_TokenRevocation_AfterLogout_TokenIsRejectedWith401`: Đăng xuất thu hồi token, sau đó dùng lại token cũ $\rightarrow$ Middleware chặn với `401 Unauthorized` (`TOKEN_REVOKED`).
  7. `Test7_RateLimiting_LoginBruteForce_ExceedingThreshold_Returns429`: Gửi liên tiếp 6 request login sai từ 1 IP $\rightarrow$ Request thứ 6 nhận `429 Too Many Requests` và sinh log SOC `RATE_LIMIT_EXCEEDED`.
  8. `Test8_SecurityHeaders_ApiResponse_ContainsDefensiveHeaders`: Kiểm tra response API chứa đầy đủ `nosniff`, `DENY`, `no-referrer` và đã ẩn hoàn toàn header `Server`.

---

## 4. KẾT QUẢ NGHIỆM THU THỰC TẾ & BẰNG CHỨNG THỰC NGHIỆM

### 4.1. Kết quả biên dịch Solution (`dotnet build`)
```text
  Determining projects to restore...
  All projects are up-to-date for restore.
  EcommerceSecurityApi -> bin\Debug\net8.0\EcommerceSecurityApi.dll
  EcommerceSecurityApi.Tests -> bin\Debug\net8.0\EcommerceSecurityApi.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.12
```

### 4.2. Kết quả kiểm thử tự động xUnit (`dotnet test`)
```text
Test run for EcommerceSecurityApi.Tests.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.1 (x64)

Passed Test1_BOLA_GetOtherUserOrder_Returns403_AndCreatesCriticalSecurityLog    [48 ms]
Passed Test2_BFLA_CustomerAccessAdminEndpoint_Returns403_AndCreatesSecurityLog [1024 ms]
Passed Test3_BFLA_StaffDeleteProduct_Returns403_AndCreatesSecurityLog          [8 ms]
Passed Test4_DataTampering_CreateOrder_UsesServerProductPrice_IgnoresTampered  [90 ms]
Passed Test5_AccountSuspension_LockedUserWithValidToken_BlockedImmediately     [3 ms]
Passed Test6_TokenRevocation_AfterLogout_TokenIsRejectedWith401                [12 ms]
Passed Test7_RateLimiting_LoginBruteForce_ExceedingThreshold_Returns429        [62 ms]
Passed Test8_SecurityHeaders_ApiResponse_ContainsDefensiveHeaders             [71 ms]

Test Run Successful.
Total tests: 8
     Passed: 8
     Failed: 0
    Skipped: 0
 Total time: 2.7685 Seconds
```

---

## 5. DANH MỤC TỆP TIN THAY ĐỔI & TẠO MỚI

### Tệp tin tạo mới:
1. `ĐACN\EcommerceSecurityApi\Helpers\ClientInfoHelper.cs` (Trích xuất IP nguồn thông minh)
2. `ĐACN\EcommerceSecurityApi\Services\ITokenBlacklistService.cs` (Interface thu hồi token)
3. `ĐACN\EcommerceSecurityApi\Services\TokenBlacklistService.cs` (Triển khai blacklist qua MemoryCache)
4. `ĐACN\EcommerceSecurityApi\Middlewares\SecurityHeadersMiddleware.cs` (Chèn Security Headers)
5. `ĐACN\EcommerceSecurityApi\Middlewares\GlobalExceptionHandler.cs` (Xử lý biệt lệ an toàn .NET 8)
6. `ĐACN\EcommerceSecurityApi.Tests\EcommerceSecurityApi.Tests.csproj` (Project kiểm thử xUnit)
7. `ĐACN\EcommerceSecurityApi.Tests\CustomWebApplicationFactory.cs` (Môi trường test InMemory độc lập)
8. `ĐACN\EcommerceSecurityApi.Tests\SecurityTests.cs` (8 kịch bản kiểm thử an ninh tự động)

### Tệp tin cập nhật:
1. `ĐACN\EcommerceSecurityApi\Program.cs` (Tích hợp RateLimiting, OnForbidden, Kestrel Hardening, ExceptionHandler, Blacklist DI)
2. `ĐACN\EcommerceSecurityApi\Controllers\AuthController.cs` (Thêm API Logout thu hồi token, gắn RateLimit)
3. `ĐACN\EcommerceSecurityApi\Controllers\OrdersController.cs` (Gắn RateLimit, Transaction an toàn, gộp ProductId, State Machine)
4. `ĐACN\EcommerceSecurityApi\Middlewares\ActiveUserMiddleware.cs` (Bổ sung kiểm tra Token Blacklist trước khi truy vấn DB)
5. `ĐACN\EcommerceSecurityApi\appsettings.json` (Cấu hình CORS AllowedOrigins)
6. `ĐACN\EcommerceSecurityApi\wwwroot\index.html` (Cập nhật hàm `handleLogout` gọi API thu hồi token)
7. `ĐACN\EcommerceSecurityApi.slnx` (Liên kết project kiểm thử vào Solution)

---

## 6. HƯỚNG DẪN KIỂM CHỨNG & DEMO BẢO VỆ ĐỒ ÁN

### 6.1. Lệnh kiểm tra biên dịch & kiểm thử an ninh
Từ thư mục gốc dự án (`DoAnChuyenNganh\ĐACN`), mở PowerShell và thực thi:
```powershell
# 1. Kiểm tra biên dịch toàn bộ Solution (Đảm bảo 0 lỗi)
dotnet build EcommerceSecurityApi.slnx

# 2. Chạy bộ kiểm thử an ninh tự động 8/8 test cases
dotnet test EcommerceSecurityApi.Tests\EcommerceSecurityApi.Tests.csproj --verbosity normal
```

### 6.2. Kiểm tra trực quan trên Web & SOC Dashboard
1. Khởi động Web API: `dotnet run --project EcommerceSecurityApi\EcommerceSecurityApi.csproj`
2. Mở trình duyệt truy cập: `http://localhost:5000` (hoặc qua link `cloudflared` nếu chạy `start_public_web.bat`).
3. **Demo Rate Limiting:** Thử nhập sai mật khẩu đăng nhập liên tiếp 6 lần $\rightarrow$ Trình duyệt hiển thị thông báo chặn `429 Too Many Requests`. Chuyển sang tab **SOC Dashboard** (đăng nhập bằng `admin`) sẽ thấy log `RATE_LIMIT_EXCEEDED` màu đỏ (`Critical`).
4. **Demo Token Revocation:** Đăng nhập tài khoản bất kỳ $\rightarrow$ Bấm nút "Đăng xuất" $\rightarrow$ Dùng công cụ (như Postman hoặc Console DevTools) gửi lại đúng mã Token đó tới `GET /api/auth/me` $\rightarrow$ Server chặn ngay với `HTTP 401 Unauthorized` (`TOKEN_REVOKED`).
5. **Demo Security Headers:** Bật `F12` $\rightarrow$ tab `Network` $\rightarrow$ chọn bất kỳ request API nào $\rightarrow$ xem `Response Headers` sẽ thấy có đủ `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` và không còn header `Server: Kestrel`.
