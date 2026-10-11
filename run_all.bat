@echo off
chcp 65001 >nul
echo ==========================================================================
echo  KHOI DONG HE THONG THUC NGHIEM AN TOAN THONG TIN (HUTECH)
echo  Sinh vien: Vo Le Nhat Hoang - MSSV: 2387700017 - Lop: 23DATA1
echo ==========================================================================

echo [1/3] Kiem tra va giai phong cong 5001 va 5002 neu dang bi chiem...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5001 ^| findstr LISTENING') do taskkill /f /pid %%a >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5002 ^| findstr LISTENING') do taskkill /f /pid %%a >nul 2>&1

echo [2/3] Dang khoi chay WebApi.V1_Vulnerable (Port 5001)...
start "WebApi.V1_Vulnerable (Port 5001)" powershell -NoExit -Command "dotnet run --project WebApi.V1_Vulnerable/WebApi.V1_Vulnerable.csproj"

timeout /t 3 /nobreak >nul

echo [3/3] Dang khoi chay WebApi.V2_Secure (Port 5002)...
start "WebApi.V2_Secure (Port 5002)" powershell -NoExit -Command "dotnet run --project WebApi.V2_Secure/WebApi.V2_Secure.csproj"

timeout /t 3 /nobreak >nul

echo.
echo ==========================================================================
echo  Dang mo Giao dien Web Dashboard & Simulator tren trinh duyet...
echo ==========================================================================
start http://localhost:5001
start http://localhost:5002

echo.
echo Hoan tat! Ca 2 ung dung da duoc khoi chay.
echo  - Web Dashboard V1 (Baseline): http://localhost:5001  (Swagger: /swagger)
echo  - Web Dashboard V2 (Secure):   http://localhost:5002  (Swagger: /swagger)
