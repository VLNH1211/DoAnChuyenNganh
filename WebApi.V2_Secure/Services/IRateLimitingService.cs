namespace WebApi.V2_Secure.Services
{
    public interface IRateLimitingService
    {
        bool IsAllowed(string key, int limit, TimeSpan window, out int retryAfterSeconds);
        bool IsLoginBlocked(string ip, string username, out int retryAfterSeconds);
        void RecordFailedLogin(string ip, string username);
        void ResetFailedLogin(string ip, string username);
    }
}
