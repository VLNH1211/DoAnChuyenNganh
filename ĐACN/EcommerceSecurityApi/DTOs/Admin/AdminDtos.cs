namespace EcommerceSecurityApi.DTOs.Admin;

public class SecurityLogDto
{
    public int Id { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string TargetResource { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
    public bool IsUserActive { get; set; } = true;
}

public class UserAdminDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
}
