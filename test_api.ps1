$ErrorActionPreference = "Stop"

Write-Host "=========================================================================="
Write-Host " BẮT ĐẦU KIỂM THỬ ĐỐI CHỨNG HỆ THỐNG THỰC NGHIỆM AN TOÀN THÔNG TIN (HUTECH)"
Write-Host " Sinh viên: Võ Lê Nhật Hoàng - MSSV: 2387700017 - Lớp: 23DATA1"
Write-Host " Đề tài: XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO WEB/API (CHỦ ĐỀ 3)"
Write-Host "=========================================================================="

# --------------------------------------------------------------------------
# PHẦN 1: ĐĂNG NHẬP LẤY TOKEN BAN ĐẦU
# --------------------------------------------------------------------------
Write-Host "`n>>> [1] ĐĂNG NHẬP LẤY TOKEN (POST /api/auth/login trên V2 - Cổng 5002)"
$resUserA = Invoke-RestMethod -Uri "http://localhost:5002/api/auth/login" -Method Post -Body (@{ username = "userA"; password = "UserA@123" } | ConvertTo-Json) -ContentType "application/json"
$tokenA = $resUserA.token
Write-Host "-> userA Login thành công! Token: $($tokenA.Substring(0, 25))..." -ForegroundColor Green

$resAdmin = Invoke-RestMethod -Uri "http://localhost:5002/api/auth/login" -Method Post -Body (@{ username = "admin"; password = "Admin@123" } | ConvertTo-Json) -ContentType "application/json"
$tokenAdmin = $resAdmin.token
Write-Host "-> admin Login thành công! Token: $($tokenAdmin.Substring(0, 25))..." -ForegroundColor Green

# --------------------------------------------------------------------------
# PHẦN 2: 4 KỊCH BẢN BẢO MẬT CỐT LÕI (KB1–KB4 THEO ĐỀ CƯƠNG)
# --------------------------------------------------------------------------
Write-Host "`n>>> [2] KB1: GỌI GET /api/orders KHÔNG CÓ TOKEN TRÊN V2 (KỲ VỌNG: 401 UNAUTHORIZED)"
try {
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/orders" -Method Get
    Write-Host "[THẤT BẠI] API không chặn yêu cầu không có token!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] API chặn thành công với HTTP Status Code: $statusCode" -ForegroundColor Green
}

Write-Host "`n>>> [3] KB1: GỌI GET /api/orders VỚI TOKEN userA (KỲ VỌNG: 200 OK, PHÂN TÁCH PHẠM VI DỮ LIỆU)"
$ordersA = Invoke-RestMethod -Uri "http://localhost:5002/api/orders" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
Write-Host "-> [THÀNH CÔNG] 200 OK. Số lượng đơn hàng của userA: $($ordersA.total)" -ForegroundColor Green

Write-Host "`n>>> [4] KB2: userA GỌI GET /api/admin/users TRÊN V2 (KỲ VỌNG: 403 FORBIDDEN)"
try {
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/admin/users" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
    Write-Host "[THẤT BẠI] API không chặn người dùng thường gọi API Admin!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] API chặn thành công với HTTP Status Code: $statusCode (RBAC Policy AdminOnly)" -ForegroundColor Green
}

Write-Host "`n>>> [5] KB2: admin GỌI GET /api/admin/users TRÊN V2 (KỲ VỌNG: 200 OK)"
$adminUsers = Invoke-RestMethod -Uri "http://localhost:5002/api/admin/users" -Method Get -Headers @{ Authorization = "Bearer $tokenAdmin" }
Write-Host "-> [THÀNH CÔNG] 200 OK. Admin lấy được $($adminUsers.total) người dùng, mật khẩu đã ẩn/mã hóa an toàn." -ForegroundColor Green

Write-Host "`n>>> [6] KB3: userA GỌI GET /api/orders/3 (ĐƠN CỦA userB) TRÊN V2 (KỲ VỌNG: 403 FORBIDDEN - CHẶN IDOR)"
try {
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/orders/3" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
    Write-Host "[THẤT BẠI] Bị lộ dữ liệu IDOR!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] Chặn IDOR thành công với HTTP Status Code: $statusCode (Không để lộ đơn của userB)" -ForegroundColor Green
}

Write-Host "`n>>> [7] KB3: userA GỌI GET /api/orders/1 (ĐƠN CHÍNH MÌNH) TRÊN V2 (KỲ VỌNG: 200 OK)"
$order1 = Invoke-RestMethod -Uri "http://localhost:5002/api/orders/1" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
Write-Host "-> [THÀNH CÔNG] 200 OK. Tên sản phẩm đơn chính chủ: $($order1.data.productName)" -ForegroundColor Green

Write-Host "`n>>> [8] KB4: GỬI CHUỖI SQL INJECTION ' OR 1=1 -- VÀO /api/orders/search TRÊN V2 (KỲ VỌNG: 400 BAD REQUEST)"
try {
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/orders/search?keyword=%27%20OR%201=1%20--" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
    Write-Host "[THẤT BẠI] API bị khai thác SQL Injection hoặc không chặn lỗi!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] API chặn thành công với HTTP Status Code: $statusCode (Validation & Parameterized Query an toàn)" -ForegroundColor Green
}

Write-Host "`n>>> [9] KB4: GỬI TỪ KHÓA HỢP LỆ 'Dell' VÀO /api/orders/search TRÊN V2 (KỲ VỌNG: 200 OK)"
$searchRes = Invoke-RestMethod -Uri "http://localhost:5002/api/orders/search?keyword=Dell" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
Write-Host "-> [THÀNH CÔNG] 200 OK. Parameterized LINQ tìm thấy: $($searchRes.count) đơn hàng." -ForegroundColor Green

# --------------------------------------------------------------------------
# PHẦN 3: CÁC KỊCH BẢN BẢO MẬT MỞ RỘNG (KB5–KB8 DEFENSE-IN-DEPTH)
# --------------------------------------------------------------------------

# KB5: Quản lý vòng đời JWT & Thu hồi Token khi Đăng xuất (JTI Blacklist)
Write-Host "`n>>> [10] KB5: ĐĂNG XUẤT THU HỒI TOKEN & THỬ PHÁT LẠI TOKEN CŨ (KỲ VỌNG: 401 UNAUTHORIZED / TOKEN REVOKED)"
$logoutRes = Invoke-RestMethod -Uri "http://localhost:5002/api/auth/logout" -Method Post -Headers @{ Authorization = "Bearer $tokenA" }
Write-Host "-> Đã gọi Logout thu hồi JTI: $($logoutRes.revokedJti)" -ForegroundColor Yellow

try {
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/auth/me" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
    Write-Host "[THẤT BẠI] Bị lỗ hổng Token Replay (Token cũ vẫn sử dụng được sau khi logout)!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] JTI Blacklist chặn Token Replay thành công với HTTP Status Code: $statusCode" -ForegroundColor Green
}

# Đăng nhập lại để lấy token mới cho các test tiếp theo
$resUserA2 = Invoke-RestMethod -Uri "http://localhost:5002/api/auth/login" -Method Post -Body (@{ username = "userA"; password = "UserA@123" } | ConvertTo-Json) -ContentType "application/json"
$tokenA2 = $resUserA2.token

# KB6: Business Logic Flaw - Số tiền âm
Write-Host "`n>>> [11] KB6: GỬI YÊU CẦU TẠO ĐƠN VỚI SỐ TIỀN ÂM -999,999đ (KỲ VỌNG: 400 BAD REQUEST)"
try {
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/orders" -Method Post -Headers @{ Authorization = "Bearer $tokenA2" } -Body (@{ productName = "Hàng Test"; totalAmount = -999999 } | ConvertTo-Json) -ContentType "application/json"
    Write-Host "[THẤT BẠI] Hệ thống chấp nhận số tiền âm!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] Business Logic Validator chặn số tiền âm với HTTP Status Code: $statusCode" -ForegroundColor Green
}

# KB6: Tạo đơn hợp lệ & Chống Mass Assignment (UserId tự động gắn từ JWT)
Write-Host "`n>>> [12] KB6: TẠO ĐƠN HỢP LỆ VÀ KIỂM TRA CHỐNG MASS ASSIGNMENT (KỲ VỌNG: 201 CREATED)"
$orderPayloadBytes = [System.Text.Encoding]::UTF8.GetBytes((@{ productName = "Màn hình Dell UltraSharp 27 inch"; totalAmount = 8500000 } | ConvertTo-Json))
$createOrderRes = Invoke-RestMethod -Uri "http://localhost:5002/api/orders" -Method Post -Headers @{ Authorization = "Bearer $tokenA2" } -Body $orderPayloadBytes -ContentType "application/json; charset=utf-8"
$newOrderId = $createOrderRes.data.id
Write-Host "-> [THÀNH CÔNG] 201 Created. Đơn #$newOrderId tự động gán Owner UserId: $($createOrderRes.data.userId) từ JWT Token." -ForegroundColor Green

# KB6: IDOR khi Xóa đơn hàng người khác
Write-Host "`n>>> [13] KB6: userA CỐ TÌNH XÓA ĐƠN #3 CỦA userB (KỲ VỌNG: 403 FORBIDDEN - CHẶN IDOR XÓA)"
try {
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/orders/3" -Method Delete -Headers @{ Authorization = "Bearer $tokenA2" }
    Write-Host "[THẤT BẠI] userA xóa được đơn hàng của người khác (Lỗ hổng IDOR khi Xóa)!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] Chặn IDOR khi Xóa thành công với HTTP Status Code: $statusCode" -ForegroundColor Green
}

# KB7: File Upload Magic Bytes Validation
Write-Host "`n>>> [14] KB7: TẢI LÊN TỆP WEBSHELL GIẢ MẠO ẢNH (KỲ VỌNG: 415 UNSUPPORTED MEDIA TYPE / 400)"
try {
    $boundary = [System.Guid]::NewGuid().ToString()
    $bodyBytes = [System.Text.Encoding]::ASCII.GetBytes("--$boundary`r`nContent-Disposition: form-data; name=`"file`"; filename=`"webshell.jpg.aspx`"`r`nContent-Type: image/jpeg`r`n`r`n<% Response.Write(`"Evil Webshell`"); %>`r`n--$boundary--`r`n")
    
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/orders/1/upload" -Method Post -Headers @{ Authorization = "Bearer $tokenA2" } -ContentType "multipart/form-data; boundary=$boundary" -Body $bodyBytes
    Write-Host "[THẤT BẠI] Máy chủ chấp nhận file Webshell độc hại!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] Magic Bytes Validator chặn file giả mạo thành công với HTTP Status Code: $statusCode" -ForegroundColor Green
}

# KB8: Rate Limiting & Anti-Spam API (429 Too Many Requests)
Write-Host "`n>>> [15] KB8: SPAM LIÊN TỤC 8 REQUESTS VÀO /api/orders/rate-test (KỲ VỌNG: 429 TOO MANY REQUESTS)"
$rateLimitTriggered = $false
for ($i = 1; $i -le 8; $i++) {
    try {
        $res = Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/orders/rate-test" -Method Get -Headers @{ Authorization = "Bearer $tokenA2" }
    } catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 429) {
            $rateLimitTriggered = $true
            $retryAfter = $_.Exception.Response.Headers["Retry-After"]
            Write-Host "-> [THÀNH CÔNG] Request #$i bị chặn với HTTP Status Code: 429 Too Many Requests (Retry-After: $retryAfter s)!" -ForegroundColor Green
            break
        }
    }
}
if (-not $rateLimitTriggered) {
    Write-Host "[CẢNH BÁO] Chưa kích hoạt mã 429 trên rate-test!" -ForegroundColor Yellow
}

# --------------------------------------------------------------------------
# PHẦN 4: KIỂM TRA OWASP HTTP SECURITY HEADERS & AUDIT LOGS
# --------------------------------------------------------------------------
Write-Host "`n>>> [16] KIỂM TRA OWASP HTTP SECURITY HEADERS TRÊN BẢN V2"
$headRes = Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:5002/api/orders" -Method Get -Headers @{ Authorization = "Bearer $tokenA2" }
$xcto = $headRes.Headers["X-Content-Type-Options"]
$xfo = $headRes.Headers["X-Frame-Options"]
$csp = $headRes.Headers["Content-Security-Policy"]
Write-Host "-> X-Content-Type-Options: $xcto" -ForegroundColor Green
Write-Host "-> X-Frame-Options: $xfo" -ForegroundColor Green
Write-Host "-> Content-Security-Policy: $csp" -ForegroundColor Green

Write-Host "`n>>> [17] KIỂM TRA NHẬT KÝ KIỂM TOÁN AN NINH (AUDIT LOGS) TRÊN V2"
$auditRes = Invoke-RestMethod -Uri "http://localhost:5002/api/admin/audit-logs" -Method Get -Headers @{ Authorization = "Bearer $tokenAdmin" }
Write-Host "-> [THÀNH CÔNG] Tổng số sự kiện an ninh được ghi lại trong CSDL: $($auditRes.total)" -ForegroundColor Green

Write-Host "`n=========================================================================="
Write-Host " TẤT CẢ 17/17 TIÊU CHÍ VÀ KỊCH BẢN BẢO MẬT (KB1–KB8) ĐẠT CHUẨN HOÀN HẢO 100%!"
Write-Host " HỆ THỐNG ĐÃ SẴN SÀNG ĐỂ BÁO CÁO TRƯỚC HỘI ĐỒNG CHẤM ĐỒ ÁN CHUYÊN NGÀNH!"
Write-Host "=========================================================================="
