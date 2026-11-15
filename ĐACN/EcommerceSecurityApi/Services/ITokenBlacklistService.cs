namespace EcommerceSecurityApi.Services;

public interface ITokenBlacklistService
{
    /// <summary>
    /// Thu hồi JWT Token bằng cách lưu JTI vào Blacklist đến thời điểm hết hạn (Exp)
    /// </summary>
    void RevokeToken(string jti, DateTime? expiresAt);

    /// <summary>
    /// Kiểm tra xem JTI của Token đã bị thu hồi hay chưa
    /// </summary>
    bool IsTokenRevoked(string jti);
}
