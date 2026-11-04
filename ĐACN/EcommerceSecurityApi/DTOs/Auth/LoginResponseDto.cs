namespace EcommerceSecurityApi.DTOs.Auth;

public class LoginResponseDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = new();
}
