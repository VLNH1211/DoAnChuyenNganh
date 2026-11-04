using System.ComponentModel.DataAnnotations;

namespace EcommerceSecurityApi.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Tên đăng nhập hoặc Email không được để trống.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên đăng nhập hoặc Email phải từ 3 đến 100 ký tự.")]
    [RegularExpression(@"^[a-zA-Z0-9_.@+-]+$", ErrorMessage = "Tên đăng nhập hoặc Email chứa ký tự không hợp lệ.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có độ dài từ 6 đến 100 ký tự.")]
    public string Password { get; set; } = string.Empty;
}
