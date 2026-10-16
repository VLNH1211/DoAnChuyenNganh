# Script PowerShell tự động khởi chạy 2 server và quay 2 video Demo thực tế riêng biệt
$ErrorActionPreference = "SilentlyContinue"

Write-Host "=========================================================================="
Write-Host " TỰ ĐỘNG CHẠY HỆ THỐNG VÀ QUAY 2 VIDEO DEMO THỰC TẾ RIÊNG BIỆT (HUTECH ATTT)"
Write-Host " Sinh viên: Võ Lê Nhật Hoàng - MSSV: 2387700017 - Lớp: 23DATA1"
Write-Host "=========================================================================="

$baseDir = "c:\Users\volen\OneDrive\Documents\DoAnChuyenNganh\ĐACN"
$pythonExe = "C:\Users\volen\AppData\Local\Python\pythoncore-3.14-64\python.exe"

# 1. Khởi động Web V1 (Cổng 5001) nếu chưa chạy
$v1Running = Get-NetTCPConnection -LocalPort 5001 -State Listen -ErrorAction SilentlyContinue
if (-not $v1Running) {
    Write-Host "`n[1/3] Đang khởi động Web V1 (Baseline) trên http://localhost:5001..." -ForegroundColor Yellow
    Start-Process dotnet -ArgumentList "run --project `"$baseDir\WebApi.V1_Vulnerable\WebApi.V1_Vulnerable.csproj`" --urls=http://localhost:5001" -WorkingDirectory "$baseDir\WebApi.V1_Vulnerable" -WindowStyle Hidden
} else {
    Write-Host "`n[1/3] Web V1 đang chạy sẵn sàng tại http://localhost:5001." -ForegroundColor Green
}

# 2. Khởi động Web V2 (Cổng 5002) nếu chưa chạy
$v2Running = Get-NetTCPConnection -LocalPort 5002 -State Listen -ErrorAction SilentlyContinue
if (-not $v2Running) {
    Write-Host "[2/3] Đang khởi động Web V2 (Hardened) trên http://localhost:5002..." -ForegroundColor Yellow
    Start-Process dotnet -ArgumentList "run --project `"$baseDir\WebApi.V2_Secure\WebApi.V2_Secure.csproj`" --urls=http://localhost:5002" -WorkingDirectory "$baseDir\WebApi.V2_Secure" -WindowStyle Hidden
} else {
    Write-Host "[2/3] Web V2 đang chạy sẵn sàng tại http://localhost:5002." -ForegroundColor Green
}

Write-Host "Đang chờ 5 giây để 2 server sẵn sàng kết nối..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

# 3. Kích hoạt Python Playwright quay 2 video riêng biệt
Write-Host "[3/3] Đang tiến hành quay 2 video Demo chuẩn từng thao tác..." -ForegroundColor Cyan
& $pythonExe "$baseDir\record_two_detailed_videos.py"

Write-Host "`n=========================================================================="
Write-Host " QUÁ TRÌNH QUAY 2 VIDEO DEMO ĐÃ HOÀN TẤT THÀNH CÔNG!"
Write-Host " File Video 1: $baseDir\videos\VIDEO_01_WEB_V1_VULNERABLE_BEFORE.webm"
Write-Host " File Video 2: $baseDir\videos\VIDEO_02_WEB_V2_SECURE_AFTER.webm"
Write-Host "=========================================================================="
