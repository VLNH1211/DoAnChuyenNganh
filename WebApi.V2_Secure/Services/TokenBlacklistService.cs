using System.Collections.Concurrent;

namespace WebApi.V2_Secure.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        // Lưu trữ danh sách JTI kèm thời điểm hết hạn
        private readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

        public void RevokeToken(string jti, DateTime expiry)
        {
            if (string.IsNullOrWhiteSpace(jti)) return;
            
            // Xóa dọn bớt token đã quá hạn tự nhiên
            CleanupExpiredTokens();

            _revokedTokens[jti] = expiry > DateTime.UtcNow ? expiry : DateTime.UtcNow.AddMinutes(60);
        }

        public bool IsTokenRevoked(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti)) return false;

            if (_revokedTokens.TryGetValue(jti, out var expiry))
            {
                if (DateTime.UtcNow <= expiry)
                {
                    return true;
                }
                else
                {
                    _revokedTokens.TryRemove(jti, out _);
                }
            }

            return false;
        }

        private void CleanupExpiredTokens()
        {
            if (_revokedTokens.Count < 100) return;

            var now = DateTime.UtcNow;
            foreach (var kvp in _revokedTokens)
            {
                if (kvp.Value < now)
                {
                    _revokedTokens.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
