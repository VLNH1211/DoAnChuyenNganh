using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.V1_Vulnerable.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string RoleName { get; set; } = string.Empty; // Admin, NhanVien, NguoiDung

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
    }
}
