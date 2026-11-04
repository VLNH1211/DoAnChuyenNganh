namespace EcommerceSecurityApi.Entities;

public class SecurityLog
{
    public int Id { get; set; }
    public string IncidentType { get; set; } = string.Empty; // BOLA_IDOR_VIOLATION, BRUTE_FORCE, UNAUTHORIZED_ACCESS
    public string Severity { get; set; } = "Warning"; // Info, Warning, Critical
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string TargetResource { get; set; } = string.Empty; // GET /api/orders/2
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; } = false;
}
