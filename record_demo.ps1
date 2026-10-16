# Script tự động khởi chạy hệ thống và quay video Demo Full HD 1920x1080
$ErrorActionPreference = "SilentlyContinue"

Write-Host "=========================================================================="
Write-Host " TỰ ĐỘNG CHẠY HỆ THỐNG VÀ QUAY VIDEO DEMO THỰC NGHIỆM ĐỒ ÁN (HUTECH ATTT)"
Write-Host " Sinh viên: Võ Lê Nhật Hoàng - MSSV: 2387700017 - Lớp: 23DATA1"
Write-Host "=========================================================================="

$baseDir = "c:\Users\volen\OneDrive\Documents\DoAnChuyenNganh\ĐACN"
$pythonExe = "C:\Users\volen\AppData\Local\Python\pythoncore-3.14-64\python.exe"

# 1. Kiểm tra và khởi động Web V1 (Cổng 5001) nếu chưa bật
$v1Running = Get-NetTCPConnection -LocalPort 5001 -State Listen -ErrorAction SilentlyContinue
if (-not $v1Running) {
    Write-Host "`n[1/3] Đang khởi động Web V1 (Baseline) trên http://localhost:5001..." -ForegroundColor Yellow
    Start-Process dotnet -ArgumentList "run --project `"$baseDir\WebApi.V1_Vulnerable\WebApi.V1_Vulnerable.csproj`" --urls=http://localhost:5001" -WorkingDirectory "$baseDir\WebApi.V1_Vulnerable" -WindowStyle Hidden
} else {
    Write-Host "`n[1/3] Web V1 đang chạy sẵn sàng tại http://localhost:5001." -ForegroundColor Green
}

# 2. Kiểm tra và khởi động Web V2 (Cổng 5002) nếu chưa bật
$v2Running = Get-NetTCPConnection -LocalPort 5002 -State Listen -ErrorAction SilentlyContinue
if (-not $v2Running) {
    Write-Host "[2/3] Đang khởi động Web V2 (Hardened) trên http://localhost:5002..." -ForegroundColor Yellow
    Start-Process dotnet -ArgumentList "run --project `"$baseDir\WebApi.V2_Secure\WebApi.V2_Secure.csproj`" --urls=http://localhost:5002" -WorkingDirectory "$baseDir\WebApi.V2_Secure" -WindowStyle Hidden
} else {
    Write-Host "[2/3] Web V2 đang chạy sẵn sàng tại http://localhost:5002." -ForegroundColor Green
}

Write-Host "Đang chờ 5 giây để 2 server khởi tạo CSDL và ổn định kết nối..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

# 3. Kích hoạt Playwright thực hiện Demo và quay video
Write-Host "[3/3] Đang thực thi kịch bản Demo và quay Video Full HD 1920x1080..." -ForegroundColor Cyan
& $pythonExe "$baseDir\record_demo_video.py"

Write-Host "`n=========================================================================="
Write-Host " QUÁ TRÌNH QUAY VIDEO DEMO ĐÃ HOÀN TẤT THÀNH CÔNG!"
Write-Host " File video lưu tại: $baseDir\videos\DEMO_THUC_NGHIEM_AN_TOAN_THONG_TIN.webm"
Write-Host "=========================================================================="
