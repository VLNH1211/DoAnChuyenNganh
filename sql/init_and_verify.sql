-- =========================================================================================
-- ĐỒ ÁN CHUYÊN NGÀNH AN TOÀN THÔNG TIN - HUTECH
-- Đề tài: XÂY DỰNG VÀ KIỂM THỬ CÁC CƠ CHẾ BẢO MẬT CHO ỨNG DỤNG WEB/API 
--         DỰA TRÊN XÁC THỰC, PHÂN QUYỀN VÀ KIỂM SOÁT TRUY CẬP
--
-- Sinh viên:  Võ Lê Nhật Hoàng
-- MSSV:       2387700017
-- Lớp:        23DATA1
-- Khoa:       Công nghệ thông tin - Chuyên ngành An toàn thông tin
-- Hệ CSDL:    Microsoft SQL Server (LocalDB / Express / Enterprise)
-- =========================================================================================

-- =========================================================================================
-- PHẦN 1: TẠO VÀ THIẾT LẬP CƠ SỞ DỮ LIỆU BẢN V1 (SecurityLab_V1 - Cố ý thiếu bảo mật)
-- =========================================================================================
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SecurityLab_V1')
BEGIN
    CREATE DATABASE SecurityLab_V1;
    PRINT N'[V1] Đã tạo cơ sở dữ liệu SecurityLab_V1 thành công.';
END
GO

USE SecurityLab_V1;
GO

-- 1.1 Tạo bảng Roles
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(30) NOT NULL UNIQUE,
        Description NVARCHAR(200) NULL
    );
END
GO

-- 1.2 Tạo bảng Users (V1: Lưu plaintext password - Minh họa rủi ro lưu trữ)
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        Password NVARCHAR(100) NOT NULL,
        FullName NVARCHAR(100) NULL,
        Role NVARCHAR(30) NOT NULL DEFAULT 'NguoiDung',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- 1.3 Tạo bảng Orders
IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderCode NVARCHAR(50) NOT NULL UNIQUE,
        UserId INT NOT NULL,
        CustomerName NVARCHAR(100) NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Orders_Users_V1 FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );
END
GO

-- 1.4 Tạo bảng AuditLogs
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Username NVARCHAR(50) NULL,
        Role NVARCHAR(30) NULL,
        Endpoint NVARCHAR(200) NOT NULL,
        HttpMethod NVARCHAR(10) NOT NULL,
        StatusCode INT NOT NULL,
        ActionDetail NVARCHAR(500) NULL,
        IpAddress NVARCHAR(50) NULL
    );
END
GO

-- 1.5 Nạp dữ liệu mẫu (Seed Data) cho SecurityLab_V1 nếu bảng trống
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (RoleName, Description) VALUES
    ('Admin', N'Quản trị viên toàn quyền hệ thống'),
    ('NhanVien', N'Nhân viên vận hành và kiểm tra'),
    ('NguoiDung', N'Khách hàng người dùng thông thường');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    INSERT INTO dbo.Users (Username, Password, FullName, Role, CreatedAt) VALUES
    ('admin', 'Admin@123', N'Hệ thống Quản Trị Viên', 'Admin', GETUTCDATE()),
    ('staff1', 'Staff@123', N'Nguyễn Văn Staff', 'NhanVien', GETUTCDATE()),
    ('userA', 'UserA@123', N'Võ Lê Nhật Hoàng', 'NguoiDung', GETUTCDATE()),
    ('userB', 'UserB@123', N'Trần Thị Bích', 'NguoiDung', GETUTCDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    DECLARE @UserA_Id INT = (SELECT Id FROM dbo.Users WHERE Username = 'userA');
    DECLARE @UserB_Id INT = (SELECT Id FROM dbo.Users WHERE Username = 'userB');

    INSERT INTO dbo.Orders (OrderCode, UserId, CustomerName, ProductName, TotalAmount, Status, CreatedAt) VALUES
    ('ORD-001', @UserA_Id, N'Võ Lê Nhật Hoàng', N'Laptop Dell XPS 15 OLED', 35000000, 'Completed', DATEADD(DAY, -10, GETUTCDATE())),
    ('ORD-002', @UserA_Id, N'Võ Lê Nhật Hoàng', N'Bàn phím cơ Akko 3087 V2 Silent', 1550000, 'Processing', DATEADD(DAY, -5, GETUTCDATE())),
    ('ORD-003', @UserB_Id, N'Trần Thị Bích', N'Màn hình LG UltraGear 27GR75Q', 7200000, 'Completed', DATEADD(DAY, -8, GETUTCDATE())),
    ('ORD-004', @UserB_Id, N'Trần Thị Bích', N'Chuột không dây Logitech MX Master 3S', 2490000, 'Pending', DATEADD(DAY, -3, GETUTCDATE())),
    ('ORD-005', @UserA_Id, N'Võ Lê Nhật Hoàng', N'Tai nghe Sony WH-1000XM5', 8490000, 'Shipping', DATEADD(DAY, -1, GETUTCDATE()));
END
GO


-- =========================================================================================
-- PHẦN 2: TẠO VÀ THIẾT LẬP CƠ SỞ DỮ LIỆU BẢN V2 (SecurityLab_V2 - Đã bảo mật toàn diện)
-- =========================================================================================
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SecurityLab_V2')
BEGIN
    CREATE DATABASE SecurityLab_V2;
    PRINT N'[V2] Đã tạo cơ sở dữ liệu SecurityLab_V2 thành công.';
END
GO

USE SecurityLab_V2;
GO

-- 2.1 Tạo bảng Roles
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(30) NOT NULL UNIQUE,
        Description NVARCHAR(200) NULL
    );
END
GO

-- 2.2 Tạo bảng Users (V2: Lưu PasswordHash băm BCrypt với Salt)
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(100) NULL,
        Role NVARCHAR(30) NOT NULL DEFAULT 'NguoiDung',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- 2.3 Tạo bảng Orders
IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderCode NVARCHAR(50) NOT NULL UNIQUE,
        UserId INT NOT NULL,
        CustomerName NVARCHAR(100) NOT NULL,
        ProductName NVARCHAR(200) NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_Orders_Users_V2 FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );
END
GO

-- 2.4 Tạo bảng AuditLogs (Được đánh chỉ mục để tối ưu tra cứu kiểm toán)
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Username NVARCHAR(50) NULL,
        Role NVARCHAR(30) NULL,
        Endpoint NVARCHAR(200) NOT NULL,
        HttpMethod NVARCHAR(10) NOT NULL,
        StatusCode INT NOT NULL,
        ActionDetail NVARCHAR(500) NULL,
        IpAddress NVARCHAR(50) NULL
    );
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Timestamp ON dbo.AuditLogs(Timestamp DESC);
    CREATE NONCLUSTERED INDEX IX_AuditLogs_StatusCode ON dbo.AuditLogs(StatusCode);
END
GO

-- 2.5 Nạp dữ liệu mẫu (Seed Data) cho SecurityLab_V2
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (RoleName, Description) VALUES
    ('Admin', N'Quản trị viên toàn quyền hệ thống'),
    ('NhanVien', N'Nhân viên vận hành và kiểm tra'),
    ('NguoiDung', N'Khách hàng người dùng thông thường');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    -- Mật khẩu hash tương ứng với BCrypt cost 11:
    -- Admin@123: $2a$11$w8N3Q28o1jFk6e.gN9hM9.54cKk5b7sH6A5Gv4k2qP3eL7pQ0qV0i (hoặc EF Core tự sinh lúc chạy app)
    -- Dưới đây là giá trị khởi tạo; ứng dụng WebApi.V2_Secure khi chạy lần đầu sẽ tự động hash chuẩn bằng thư viện BCrypt.Net-Next
    INSERT INTO dbo.Users (Username, PasswordHash, FullName, Role, CreatedAt) VALUES
    ('admin', '$2a$11$J1Qx8P8Qx8P8Qx8P8Qx8PuYj5Kk5b7sH6A5Gv4k2qP3eL7pQ0qV0i', N'Hệ thống Quản Trị Viên', 'Admin', GETUTCDATE()),
    ('staff1', '$2a$11$K2Rx9Q9Rx9Q9Rx9Q9Rx9QuZj6Ll6c8tI7B6Hw5l3rQ4fM8qR1rW1j', N'Nguyễn Văn Staff', 'NhanVien', GETUTCDATE()),
    ('userA', '$2a$11$L3Sy0R0Sy0R0Sy0R0Sy0RvAk7Mm7d9uJ8C7Ix6m4sR5gN9sS2sX2k', N'Võ Lê Nhật Hoàng', 'NguoiDung', GETUTCDATE()),
    ('userB', '$2a$11$M4Tz1S1Tz1S1Tz1S1Tz1SwBl8Nn8e0vK9D8Jy7n5tS6hO0tT3tY3l', N'Trần Thị Bích', 'NguoiDung', GETUTCDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    DECLARE @UserA_Id_V2 INT = (SELECT Id FROM dbo.Users WHERE Username = 'userA');
    DECLARE @UserB_Id_V2 INT = (SELECT Id FROM dbo.Users WHERE Username = 'userB');

    INSERT INTO dbo.Orders (OrderCode, UserId, CustomerName, ProductName, TotalAmount, Status, CreatedAt) VALUES
    ('ORD-001', @UserA_Id_V2, N'Võ Lê Nhật Hoàng', N'Laptop Dell XPS 15 OLED', 35000000, 'Completed', DATEADD(DAY, -10, GETUTCDATE())),
    ('ORD-002', @UserA_Id_V2, N'Võ Lê Nhật Hoàng', N'Bàn phím cơ Akko 3087 V2 Silent', 1550000, 'Processing', DATEADD(DAY, -5, GETUTCDATE())),
    ('ORD-003', @UserB_Id_V2, N'Trần Thị Bích', N'Màn hình LG UltraGear 27GR75Q', 7200000, 'Completed', DATEADD(DAY, -8, GETUTCDATE())),
    ('ORD-004', @UserB_Id_V2, N'Trần Thị Bích', N'Chuột không dây Logitech MX Master 3S', 2490000, 'Pending', DATEADD(DAY, -3, GETUTCDATE())),
    ('ORD-005', @UserA_Id_V2, N'Võ Lê Nhật Hoàng', N'Tai nghe Sony WH-1000XM5', 8490000, 'Shipping', DATEADD(DAY, -1, GETUTCDATE()));
END
GO


-- =========================================================================================
-- PHẦN 3: CÁC CÂU TRUY VẤN KIỂM CHỨNG TRÊN SQL SERVER MANAGEMENT STUDIO (SSMS)
-- =========================================================================================

-- =========================================================================================
-- [TRUY VẤN 1]: KIỂM CHỨNG QUYỀN SỞ HỮU ĐƠN HÀNG (PHỤC VỤ KỊCH BẢN KB3 - BOLA/IDOR)
-- Mục đích: Đối chiếu rõ ràng đơn hàng nào thuộc về ai.
-- Kết quả chuẩn: 
--   - userA (Võ Lê Nhật Hoàng) chỉ sở hữu đơn #1, #2, #5.
--   - userB (Trần Thị Bích) sở hữu đơn #3, #4.
-- => Trên V1: userA gọi GET /api/orders/3 sẽ lộ dữ liệu đơn hàng của userB (LỖ HỔNG).
-- => Trên V2: userA gọi GET /api/orders/3 bị trả về 403 Forbidden và ghi log (BẢO VỆ).
-- =========================================================================================
USE SecurityLab_V2;
GO

SELECT 
    o.Id AS [Mã Đơn (ID)],
    o.OrderCode AS [Mã Vận Đơn],
    o.UserId AS [Mã Chủ Sở Hữu (UserId)],
    u.Username AS [Tài Khoản Chủ Sở Hữu],
    u.FullName AS [Họ Tên Khách Hàng],
    o.ProductName AS [Tên Sản Phẩm],
    FORMAT(o.TotalAmount, 'N0') + ' VND' AS [Tổng Tiền],
    o.Status AS [Trạng Thái],
    o.CreatedAt AS [Ngày Tạo]
FROM dbo.Orders o
INNER JOIN dbo.Users u ON o.UserId = u.Id
ORDER BY o.Id ASC;
GO


-- =========================================================================================
-- [TRUY VẤN 2]: KIỂM CHỨNG TÍNH TOÀN VẸN DỮ LIỆU TRƯỚC VÀ SAU TẤN CÔNG SQLi (KỊCH BẢN KB4)
-- Mục đích: Đảm bảo số lượng bản ghi và doanh thu không bị thay đổi, xóa bỏ hay thao túng.
-- Khi tấn công ' OR 1=1 -- hoặc các payload SQLi vào V2:
--   - API V2 trả về 400 Bad Request ngay lập tức.
--   - Tổng số đơn hàng vẫn nguyên vẹn 5 đơn, tổng tiền giữ nguyên 54,730,000 VND.
-- =========================================================================================
USE SecurityLab_V2;
GO

SELECT 
    COUNT(*) AS [Tổng Số Đơn Hàng],
    FORMAT(SUM(TotalAmount), 'N0') + ' VND' AS [Tổng Giá Trị Doanh Số],
    MIN(CreatedAt) AS [Đơn Đầu Tiên],
    MAX(CreatedAt) AS [Đơn Gần Nhất]
FROM dbo.Orders;
GO


-- =========================================================================================
-- [TRUY VẤN 3]: TRUY VẤN NHẬT KÝ KIỂM TOÁN AN NINH (AUDIT LOGS) TRÊN BẢN V2
-- Mục đích: Giám sát toàn bộ các nỗ lực vi phạm an ninh:
--   - 401 Unauthorized: Gọi API khi chưa có Token (KB1)
--   - 403 Forbidden: userA/staff1 gọi API Quản trị (KB2) hoặc userA cố đọc đơn của userB (KB3)
--   - 400 Bad Request: Đầu vào chứa dấu hiệu SQL Injection hoặc vượt quá ký tự (KB4)
-- =========================================================================================
USE SecurityLab_V2;
GO

SELECT TOP 50
    Id AS [Log ID],
    Timestamp AS [Thời Điểm (UTC)],
    ISNULL(Username, 'Anonymous') AS [Người Dùng],
    ISNULL(Role, 'None') AS [Vai Trò],
    HttpMethod AS [Method],
    Endpoint AS [Đường Dẫn API],
    StatusCode AS [Mã HTTP],
    ActionDetail AS [Chi Tiết Hành Động / Vi Phạm],
    IpAddress AS [Địa Chỉ IP]
FROM dbo.AuditLogs
ORDER BY Timestamp DESC;
GO


-- =========================================================================================
-- [TRUY VẤN 4]: THỐNG KÊ CÁC SỰ KIỆN AN NINH BỊ CHẶN THEO MÃ HTTP
-- =========================================================================================
USE SecurityLab_V2;
GO

SELECT 
    StatusCode AS [Mã Phản Hồi HTTP],
    CASE StatusCode
        WHEN 200 THEN N'200 OK (Truy cập hợp lệ)'
        WHEN 400 THEN N'400 Bad Request (KB4 Chặn SQL Injection / Đầu vào lỗi)'
        WHEN 401 THEN N'401 Unauthorized (KB1 Chặn chưa đăng nhập / Token thiếu)'
        WHEN 403 THEN N'403 Forbidden (KB2 Chặn phân quyền sai / KB3 Chặn IDOR)'
        ELSE N'Mã trạng thái khác'
    END AS [Ý Nghĩa Bảo Mật],
    COUNT(*) AS [Số Lần Xuất Hiện]
FROM dbo.AuditLogs
GROUP BY StatusCode
ORDER BY StatusCode ASC;
GO
