# Script khởi động đồng thời cả 2 phiên bản V1 và V2
Write-Host "=========================================================================="
Write-Host " KHỞI ĐỘNG HỆ THỐNG THỰC NGHIỆM AN TOÀN THÔNG TIN (HUTECH)"
Write-Host " Sinh viên: Võ Lê Nhật Hoàng (MSSV: 2387700017 - Lớp: 23DATA1)"
Write-Host "=========================================================================="

Write-Host "[1/3] Kiểm tra và giải phóng cổng 5001 và 5002 nếu đang bị chiếm..."
$ports = @(5001, 5002)
foreach ($port in $ports) {
    $processes = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            Stop-Process -Id $proc.OwningProcess -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "[2/3] Đang khởi chạy WebApi.V1_Vulnerable tại http://localhost:5001..."
Start-Process powershell -WorkingDirectory $PSScriptRoot -ArgumentList "-NoExit", "-Command", "Write-Host 'WebApi.V1_Vulnerable (Baseline) - Cổng 5001' -ForegroundColor Yellow; dotnet run --project WebApi.V1_Vulnerable/WebApi.V1_Vulnerable.csproj"

Start-Sleep -Seconds 3

Write-Host "[3/3] Đang khởi chạy WebApi.V2_Secure tại http://localhost:5002..."
Start-Process powershell -WorkingDirectory $PSScriptRoot -ArgumentList "-NoExit", "-Command", "Write-Host 'WebApi.V2_Secure (Hardened) - Cổng 5002' -ForegroundColor Green; dotnet run --project WebApi.V2_Secure/WebApi.V2_Secure.csproj"

Start-Sleep -Seconds 3

Write-Host "`nĐang mở giao diện Web Dashboard & Simulator trên trình duyệt..."
Start-Process "http://localhost:5001"
Start-Process "http://localhost:5002"

Write-Host "`n=========================================================================="
Write-Host " Cả 2 ứng dụng đã được khởi chạy thành công!"
Write-Host " - Web Portal V1 (Baseline): http://localhost:5001  (Swagger: /swagger)"
Write-Host " - Web Portal V2 (Secure):   http://localhost:5002  (Swagger: /swagger)"
Write-Host "=========================================================================="
