using WebApi.V2_Secure.Data;
using WebApi.V2_Secure.Models;

namespace WebApi.V2_Secure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(IServiceScopeFactory scopeFactory, ILogger<AuditLogService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task LogAsync(
            string? username,
            string? role,
            string endpoint,
            string httpMethod,
            int statusCode,
            string actionDetail,
            string? ipAddress = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<V2DbContext>();

                var log = new AuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    Username = string.IsNullOrWhiteSpace(username) ? "Anonymous" : username,
                    Role = string.IsNullOrWhiteSpace(role) ? "None" : role,
                    Endpoint = endpoint,
                    HttpMethod = httpMethod,
                    StatusCode = statusCode,
                    ActionDetail = actionDetail,
                    IpAddress = ipAddress ?? "127.0.0.1"
                };

                dbContext.AuditLogs.Add(log);
                await dbContext.SaveChangesAsync();

                // Console output formatting with security highlights
                string prefix = statusCode switch
                {
                    401 => "[AUDIT 401 UNAUTHORIZED]",
                    403 => "[AUDIT 403 FORBIDDEN / IDOR]",
                    400 => "[AUDIT 400 VALIDATION / SQLI BLOCKED]",
                    _ => "[AUDIT LOG]"
                };

                _logger.LogInformation(
                    "{Prefix} User='{User}' | Role='{Role}' | Method={Method} | Endpoint={Endpoint} | Status={StatusCode} | Detail={Detail}",
                    prefix, log.Username, log.Role, log.HttpMethod, log.Endpoint, log.StatusCode, log.ActionDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AUDIT ERROR] Không thể ghi audit log: {Message}", ex.Message);
            }
        }
    }
}
