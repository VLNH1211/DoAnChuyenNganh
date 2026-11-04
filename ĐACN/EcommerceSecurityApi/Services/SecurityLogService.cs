using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.DTOs.Admin;
using EcommerceSecurityApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Services;

public interface ISecurityLogService
{
    Task LogIncidentAsync(string incidentType, string severity, string description, string targetResource, int? userId = null, string? username = null, string? ipAddress = null);
    Task<List<SecurityLogDto>> GetRecentLogsAsync(int count = 50);
}

public class SecurityLogService : ISecurityLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SecurityLogService> _logger;

    public SecurityLogService(AppDbContext context, ILogger<SecurityLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogIncidentAsync(string incidentType, string severity, string description, string targetResource, int? userId = null, string? username = null, string? ipAddress = null)
    {
        try
        {
            var log = new SecurityLog
            {
                IncidentType = incidentType,
                Severity = severity,
                Description = description,
                TargetResource = targetResource,
                UserId = userId,
                Username = username ?? (userId.HasValue ? $"User_{userId}" : "Anonymous"),
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                IsResolved = false
            };

            await _context.SecurityLogs.AddAsync(log);
            await _context.SaveChangesAsync();

            _logger.LogWarning("🛡️ [SECURITY INCIDENT LOGGED] [{Type} - {Severity}] User: {Username} | Resource: {Resource} | {Description}",
                incidentType, severity, log.Username, targetResource, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ghi nhật ký an ninh.");
        }
    }

    public async Task<List<SecurityLogDto>> GetRecentLogsAsync(int count = 50)
    {
        var logs = await _context.SecurityLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();

        var userIds = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
        var activeStatusMap = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.IsActive);

        return logs.Select(l => new SecurityLogDto
        {
            Id = l.Id,
            IncidentType = l.IncidentType,
            Severity = l.Severity,
            UserId = l.UserId,
            Username = l.Username,
            TargetResource = l.TargetResource,
            Description = l.Description,
            IpAddress = l.IpAddress,
            CreatedAt = DateTime.SpecifyKind(l.CreatedAt, DateTimeKind.Utc),
            IsResolved = l.IsResolved,
            IsUserActive = l.UserId.HasValue && activeStatusMap.ContainsKey(l.UserId.Value) ? activeStatusMap[l.UserId.Value] : true
        }).ToList();
    }
}
