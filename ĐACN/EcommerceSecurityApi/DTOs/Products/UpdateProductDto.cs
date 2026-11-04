using System.ComponentModel.DataAnnotations;

namespace EcommerceSecurityApi.DTOs.Products;

public class UpdateProductDto
{
    [Required(ErrorMessage = "Tên sản phẩm công nghệ không được để trống.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên sản phẩm phải từ 2 đến 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Thương hiệu không được để trống.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Thương hiệu phải từ 2 đến 100 ký tự.")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giá bán không được để trống.")]
    [Range(1000, 500000000, ErrorMessage = "Giá sản phẩm phải từ 1.000đ đến 500.000.000đ.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Số lượng tồn kho không được để trống.")]
    [Range(0, 100000, ErrorMessage = "Số lượng tồn kho phải từ 0 đến 100.000.")]
    public int Stock { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
    public string? ImageUrl { get; set; }

    [StringLength(2000, ErrorMessage = "Mô tả sản phẩm tối đa 2000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Thông số kỹ thuật tối đa 500 ký tự.")]
    public string? Specification { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thể loại danh mục sản phẩm.")]
    public int CategoryId { get; set; }
}
