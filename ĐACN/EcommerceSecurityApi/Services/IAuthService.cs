using EcommerceSecurityApi.DTOs.Auth;

namespace EcommerceSecurityApi.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, LoginResponseDto? Data)> LoginAsync(LoginRequestDto request);
    Task<(bool Success, string Message, UserInfoDto? Data)> RegisterCustomerAsync(RegisterRequestDto request);
    Task<UserInfoDto?> GetCurrentUserAsync(int userId);
}
