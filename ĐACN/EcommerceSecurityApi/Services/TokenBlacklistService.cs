using Microsoft.Extensions.Caching.Memory;

namespace EcommerceSecurityApi.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenBlacklistService> _logger;
    private const string BlacklistPrefix = "revoked_jwt_";

    public TokenBlacklistService(IMemoryCache cache, ILogger<TokenBlacklistService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void RevokeToken(string jti, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(jti)) return;

        var cacheKey = $"{BlacklistPrefix}{jti}";
        var expiration = expiresAt ?? DateTime.UtcNow.AddHours(2);

        if (expiration <= DateTime.UtcNow)
        {
            // Token đã tự nhiên hết hạn, không cần tốn dung lượng cache
            return;
        }

        _cache.Set(cacheKey, true, expiration);
        _logger.LogInformation("🛡️ [TOKEN REVOKED] Token JTI: {Jti} đã bị thu hồi và lưu Blacklist đến {Expiry}.", jti, expiration);
    }

    public bool IsTokenRevoked(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti)) return false;

        var cacheKey = $"{BlacklistPrefix}{jti}";
        return _cache.TryGetValue(cacheKey, out _);
    }
}
