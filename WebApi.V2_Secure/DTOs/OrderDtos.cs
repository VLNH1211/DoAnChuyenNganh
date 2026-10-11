using System.ComponentModel.DataAnnotations;

namespace WebApi.V2_Secure.DTOs
{
    /// <summary>
    /// V2: Chống Mass Assignment - Client chỉ được gửi ProductName và TotalAmount.
    /// UserId và CustomerName được tự động trích xuất trực tiếp từ Claims của JWT Token.
    /// </summary>
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [MaxLength(200, ErrorMessage = "Tên sản phẩm không vượt quá 200 ký tự.")]
        public string ProductName { get; set; } = string.Empty;

        [Range(1000, 1000000000, ErrorMessage = "Số tiền đơn hàng phải lớn hơn 0 (tối thiểu 1.000 VNĐ và tối đa 1.000.000.000 VNĐ).")]
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// V2: Cập nhật thông tin đơn hàng có kiểm soát logic nghiệp vụ
    /// </summary>
    public class UpdateOrderRequest
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [MaxLength(200, ErrorMessage = "Tên sản phẩm không vượt quá 200 ký tự.")]
        public string ProductName { get; set; } = string.Empty;

        [Range(1000, 1000000000, ErrorMessage = "Số tiền đơn hàng phải lớn hơn 0.")]
        public decimal TotalAmount { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }
    }
}
