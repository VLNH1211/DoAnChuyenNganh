using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.V1_Vulnerable.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? Username { get; set; }

        [MaxLength(30)]
        public string? Role { get; set; }

        [Required]
        [MaxLength(200)]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string HttpMethod { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        [MaxLength(500)]
        public string? ActionDetail { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }
    }
}
