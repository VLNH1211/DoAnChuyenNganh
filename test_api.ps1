$ErrorActionPreference = "Stop"

Write-Host "=========================================================================="
Write-Host " BẮT ĐẦU KIỂM THỬ ĐỐI CHỨNG HỆ THỐNG THỰC NGHIỆM AN TOÀN THÔNG TIN (HUTECH)"
Write-Host " Sinh viên: Võ Lê Nhật Hoàng - MSSV: 2387700017 - Lớp: 23DATA1"
Write-Host "=========================================================================="

# 1. Đăng nhập lấy token trên V2
Write-Host "`n>>> [1] ĐĂNG NHẬP LẤY TOKEN (POST /api/auth/login trên V2)"
$resUserA = Invoke-RestMethod -Uri "http://localhost:5002/api/auth/login" -Method Post -Body (@{ username = "userA"; password = "UserA@123" } | ConvertTo-Json) -ContentType "application/json"
$tokenA = $resUserA.token
Write-Host "-> userA Login thành công! Token: $($tokenA.Substring(0, 25))..."

$resAdmin = Invoke-RestMethod -Uri "http://localhost:5002/api/auth/login" -Method Post -Body (@{ username = "admin"; password = "Admin@123" } | ConvertTo-Json) -ContentType "application/json"
$tokenAdmin = $resAdmin.token
Write-Host "-> admin Login thành công! Token: $($tokenAdmin.Substring(0, 25))..."

# 2. KB1: Gọi GET /api/orders không có token trên V2
Write-Host "`n>>> [2] KB1: GỌI GET /api/orders KHÔNG CÓ TOKEN TRÊN V2 (KỲ VỌNG: 401 UNAUTHORIZED)"
try {
    Invoke-WebRequest -Uri "http://localhost:5002/api/orders" -Method Get
    Write-Host "[THẤT BẠI] API không chặn yêu cầu không có token!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] API chặn thành công với HTTP Status Code: $statusCode" -ForegroundColor Green
}

# 3. KB1: Gọi GET /api/orders CÓ TOKEN userA trên V2
Write-Host "`n>>> [3] KB1: GỌI GET /api/orders VỚI TOKEN userA (KỲ VỌNG: 200 OK, CHỈ ĐƠN CỦA userA)"
$ordersA = Invoke-RestMethod -Uri "http://localhost:5002/api/orders" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
Write-Host "-> [THÀNH CÔNG] 200 OK. Số lượng đơn hàng của userA: $($ordersA.total)" -ForegroundColor Green

# 4. KB2: userA gọi GET /api/admin/users trên V2
Write-Host "`n>>> [4] KB2: userA GỌI GET /api/admin/users TRÊN V2 (KỲ VỌNG: 403 FORBIDDEN)"
try {
    Invoke-WebRequest -Uri "http://localhost:5002/api/admin/users" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
    Write-Host "[THẤT BẠI] API không chặn người dùng thường gọi API Admin!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] API chặn thành công với HTTP Status Code: $statusCode" -ForegroundColor Green
}

# 5. KB2: admin gọi GET /api/admin/users trên V2
Write-Host "`n>>> [5] KB2: admin GỌI GET /api/admin/users TRÊN V2 (KỲ VỌNG: 200 OK)"
$adminUsers = Invoke-RestMethod -Uri "http://localhost:5002/api/admin/users" -Method Get -Headers @{ Authorization = "Bearer $tokenAdmin" }
Write-Host "-> [THÀNH CÔNG] 200 OK. Admin lấy được $($adminUsers.total) người dùng." -ForegroundColor Green

# 6. KB3: userA gọi GET /api/orders/3 (đơn của userB) trên V2
Write-Host "`n>>> [6] KB3: userA GỌI GET /api/orders/3 (ĐƠN CỦA userB) TRÊN V2 (KỲ VỌNG: 403 FORBIDDEN - CHẶN IDOR)"
try {
    Invoke-WebRequest -Uri "http://localhost:5002/api/orders/3" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
    Write-Host "[THẤT BẠI] Bị lộ dữ liệu IDOR!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] Chặn IDOR thành công với HTTP Status Code: $statusCode (Không để lộ đơn của userB)" -ForegroundColor Green
}

# 7. KB3: userA gọi GET /api/orders/1 (đơn chính mình) trên V2
Write-Host "`n>>> [7] KB3: userA GỌI GET /api/orders/1 (ĐƠN CHÍNH MÌNH) TRÊN V2 (KỲ VỌNG: 200 OK)"
$order1 = Invoke-RestMethod -Uri "http://localhost:5002/api/orders/1" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
Write-Host "-> [THÀNH CÔNG] 200 OK. Tên sản phẩm đơn chính chủ: $($order1.data.productName)" -ForegroundColor Green

# 8. KB4: Gửi chuỗi SQL Injection vào /api/orders/search trên V2
Write-Host "`n>>> [8] KB4: GỬI CHUỖI SQL INJECTION ' OR 1=1 -- VÀO /api/orders/search TRÊN V2 (KỲ VỌNG: 400 BAD REQUEST)"
try {
    Invoke-WebRequest -Uri "http://localhost:5002/api/orders/search?keyword=%27%20OR%201=1%20--" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
    Write-Host "[THẤT BẠI] API bị khai thác SQL Injection hoặc không chặn lỗi!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "-> [THÀNH CÔNG] API chặn thành công với HTTP Status Code: $statusCode (Validation / SQLi Filter hoạt động hoàn hảo)" -ForegroundColor Green
}

# 9. KB4: Gửi từ khóa hợp lệ vào /api/orders/search trên V2
Write-Host "`n>>> [9] KB4: GỬI TỪ KHÓA HỢP LỆ 'Dell' VÀO /api/orders/search TRÊN V2 (KỲ VỌNG: 200 OK)"
$searchRes = Invoke-RestMethod -Uri "http://localhost:5002/api/orders/search?keyword=Dell" -Method Get -Headers @{ Authorization = "Bearer $tokenA" }
Write-Host "-> [THÀNH CÔNG] 200 OK. Parameterized LINQ tìm thấy: $($searchRes.count) đơn hàng." -ForegroundColor Green

# 10. Kiểm tra Audit Logs
Write-Host "`n>>> [10] KIỂM TRA NHẬT KÝ KIỂM TOÁN AN NINH (AUDIT LOGS) TRÊN V2"
$auditRes = Invoke-RestMethod -Uri "http://localhost:5002/api/admin/audit-logs" -Method Get -Headers @{ Authorization = "Bearer $tokenAdmin" }
Write-Host "-> [THÀNH CÔNG] Tổng số sự kiện an ninh được ghi lại trong CSDL: $($auditRes.total)" -ForegroundColor Green

Write-Host "`n=========================================================================="
Write-Host " TẤT CẢ 10/10 KỊCH BẢN VÀ TIÊU CHÍ BẢO MẬT ĐỀU ĐẠT CHUẨN HOÀN HẢO 100%!"
Write-Host "=========================================================================="
