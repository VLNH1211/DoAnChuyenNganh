using System.ComponentModel.DataAnnotations;

namespace EcommerceSecurityApi.DTOs.Orders;

public class CreateOrderItemRequestDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng mua mỗi món phải từ 1 đến 100 sản phẩm.")]
    public int Quantity { get; set; }
}

public class CreateOrderDto
{
    [Required(ErrorMessage = "Họ tên người nhận không được để trống.")]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống.")]
    [MaxLength(255)]
    public string ShippingAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Giỏ hàng không được để trống.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm công nghệ trong đơn hàng.")]
    public List<CreateOrderItemRequestDto> Items { get; set; } = new();
}
