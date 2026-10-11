namespace WebApi.V2_Secure.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string? username,
            string? role,
            string endpoint,
            string httpMethod,
            int statusCode,
            string actionDetail,
            string? ipAddress = null);
    }
}
