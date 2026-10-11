namespace WebApi.V2_Secure.Services
{
    public interface ITokenBlacklistService
    {
        void RevokeToken(string jti, DateTime expiry);
        bool IsTokenRevoked(string jti);
    }
}
