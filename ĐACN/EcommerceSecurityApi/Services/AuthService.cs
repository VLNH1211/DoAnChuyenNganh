using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.DTOs.Auth;
using EcommerceSecurityApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ISecurityLogService _securityLogService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, ITokenService tokenService, ISecurityLogService securityLogService, ILogger<AuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _securityLogService = securityLogService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request)
    {
        // 1. Tìm kiếm User theo Username hoặc Email và nạp danh sách Role
        string loginKey = request.Username.Trim();
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == loginKey || u.Email == loginKey);

        // 2. Chống User Enumeration: Thông báo chung nếu không tìm thấy User
        if (user == null)
        {
            _logger.LogWarning("Đăng nhập thất bại: Tài khoản '{Username}' không tồn tại.", request.Username);
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác.", null);
        }

        // 3. Kiểm tra tài khoản có bị khóa hay không
        if (!user.IsActive)
        {
            _logger.LogWarning("Đăng nhập thất bại: Tài khoản '{Username}' đã bị khóa.", request.Username);
            return (false, "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.", null);
        }

        // 4. Xác thực mật khẩu bằng giải thuật băm an toàn BCrypt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 3)
            {
                await _securityLogService.LogIncidentAsync(
                    incidentType: "BRUTE_FORCE_SUSPECT",
                    severity: user.FailedLoginAttempts >= 5 ? "Critical" : "Warning",
                    description: $"Phát hiện hành vi nhập sai mật khẩu {user.FailedLoginAttempts} lần liên tiếp đối với tài khoản '{user.Username}'. Gửi cảnh báo đến Quản trị viên để quyết định khóa tài khoản.",
                    targetResource: "POST /api/auth/login",
                    userId: user.Id,
                    username: user.Username
                );
            }
            await _context.SaveChangesAsync();

            _logger.LogWarning("Đăng nhập thất bại: Mật khẩu sai cho tài khoản '{Username}'. Số lần sai: {Count}", user.Username, user.FailedLoginAttempts);
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác.", null);
        }

        // 5. Đăng nhập thành công: Cập nhật thời gian đăng nhập và Reset số lần sai
        user.LastLoginAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;
        await _context.SaveChangesAsync();

        // 6. Trích xuất vai trò (Roles) và sinh JWT Token
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var token = _tokenService.GenerateJwtToken(user, roles, out var expiresAt);

        // 7. Ghi nhận nhật ký đăng nhập hệ thống (Audit Log)
        string roleList = string.Join(", ", roles);
        await _securityLogService.LogIncidentAsync(
            incidentType: "USER_LOGIN_SUCCESS",
            severity: "Info",
            description: $"Tài khoản '{user.Username}' ({user.FullName} - Vai trò: {roleList}) đăng nhập thành công vào hệ thống lúc {DateTime.UtcNow.AddHours(7):HH:mm:ss dd/MM/yyyy}.",
            targetResource: "POST /api/auth/login",
            userId: user.Id,
            username: user.Username
        );

        _logger.LogInformation("Đăng nhập thành công: Tài khoản '{Username}' với các vai trò [{Roles}].", user.Username, string.Join(", ", roles));

        var response = new LoginResponseDto
        {
            Success = true,
            Message = "Đăng nhập thành công.",
            Token = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
            }
        };

        return (true, "Đăng nhập thành công.", response);
    }

    public async Task<(bool Success, string Message, UserInfoDto? Data)> RegisterCustomerAsync(RegisterRequestDto request)
    {
        // 1. Kiểm tra Username hoặc Email đã tồn tại chưa
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return (false, "Tên đăng nhập này đã được sử dụng.", null);
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return (false, "Email này đã được đăng ký tài khoản khác.", null);
        }

        // 2. Lấy Role KhachHang mặc định
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "KhachHang");
        if (customerRole == null)
        {
            return (false, "Hệ thống chưa thiết lập vai trò Khách hàng.", null);
        }

        // 3. Tạo User mới và băm mật khẩu bằng BCrypt
        var newUser = new User
        {
            Username = request.Username,
            Email = request.Email,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Username : request.FullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();

        // Gán Role KhachHang
        await _context.UserRoles.AddAsync(new UserRole
        {
            UserId = newUser.Id,
            RoleId = customerRole.Id
        });
        await _context.SaveChangesAsync();

        var userInfo = new UserInfoDto
        {
            Id = newUser.Id,
            Username = newUser.Username,
            Email = newUser.Email,
            FullName = newUser.FullName,
            Roles = new List<string> { customerRole.Name }
        };

        // Ghi nhận nhật ký tài khoản mới được tạo (Audit Log)
        await _securityLogService.LogIncidentAsync(
            incidentType: "USER_REGISTERED",
            severity: "Info",
            description: $"Người dùng mới '{newUser.FullName}' (Tài khoản: '{newUser.Username}', Email: '{newUser.Email}') vừa đăng ký tài khoản thành công lúc {DateTime.UtcNow.AddHours(7):HH:mm:ss dd/MM/yyyy}.",
            targetResource: "POST /api/auth/register",
            userId: newUser.Id,
            username: newUser.Username
        );

        return (true, "Đăng ký tài khoản thành công.", userInfo);
    }

    public async Task<UserInfoDto?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new UserInfoDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }
}
