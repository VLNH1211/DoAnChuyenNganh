@echo off
chcp 65001 > nul
title TechSec Gear - Public Web & API
echo ===============================================================
echo     KHỞI ĐỘNG HỆ THỐNG TECHSEC GEAR & PUBLIC RA INTERNET
echo ===============================================================
echo.
echo 1. Đang khởi động Backend Server (.NET 8)...
start "Backend Server (Port 5141)" dotnet run --project "%~dp0ĐACN\EcommerceSecurityApi\EcommerceSecurityApi.csproj" --urls "http://localhost:5141"
echo    Đợi 5 giây để server sẵn sàng...
timeout /t 5 /nobreak > nul

echo.
echo 2. Đang mở đường link HTTPS công khai qua Cloudflare Tunnel...
echo    (Đường link https://....trycloudflare.com sẽ xuất hiện bên dưới)
echo.
"%~dp0ĐACN\EcommerceSecurityApi\cloudflared.exe" tunnel --url http://localhost:5141
pause