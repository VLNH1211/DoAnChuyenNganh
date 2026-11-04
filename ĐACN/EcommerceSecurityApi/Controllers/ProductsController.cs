using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.DTOs.Products;
using EcommerceSecurityApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm công nghệ (Công khai - Cho phép tìm kiếm & lọc thể loại)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] string? sortBy)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .AsQueryable();

        // 1. Tìm kiếm theo Tên sản phẩm, Thương hiệu hoặc Thông số
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) 
                                  || p.Brand.ToLower().Contains(term)
                                  || p.Specification.ToLower().Contains(term));
        }

        // 2. Lọc theo Thể loại (Category)
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // 3. Sắp xếp (Sorting)
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.Id)
        };

        var products = await query.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Brand = p.Brand,
            Price = p.Price,
            Stock = p.Stock,
            ImageUrl = p.ImageUrl,
            Description = p.Description,
            Specification = p.Specification,
            CategoryId = p.CategoryId,
            CategoryName = p.Category != null ? p.Category.Name : "Khác",
            CreatedAt = p.CreatedAt
        }).ToListAsync();

        return Ok(products);
    }

    /// <summary>
    /// Xem thông tin chi tiết một sản phẩm công nghệ (Công khai)
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Brand = p.Brand,
                Price = p.Price,
                Stock = p.Stock,
                ImageUrl = p.ImageUrl,
                Description = p.Description,
                Specification = p.Specification,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "Khác",
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (product == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy sản phẩm có mã ID {id}." });
        }

        return Ok(product);
    }

    /// <summary>
    /// Thêm sản phẩm công nghệ mới vào cửa hàng (Chỉ Quản trị viên Admin hoặc Nhân viên NhanVien)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,NhanVien")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists)
        {
            return BadRequest(new { success = false, message = "Thể loại danh mục không tồn tại trong hệ thống." });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Brand = request.Brand.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
                ? "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80"
                : request.ImageUrl.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Specification = request.Specification?.Trim() ?? string.Empty,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        var category = await _context.Categories.FindAsync(product.CategoryId);

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Brand = product.Brand,
            Price = product.Price,
            Stock = product.Stock,
            ImageUrl = product.ImageUrl,
            Description = product.Description,
            Specification = product.Specification,
            CategoryId = product.CategoryId,
            CategoryName = category?.Name ?? "Khác",
            CreatedAt = product.CreatedAt
        };

        _logger.LogInformation("Người dùng {User} đã thêm sản phẩm mới '{Name}' (ID: {Id}).", User.Identity?.Name, product.Name, product.Id);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, dto);
    }

    /// <summary>
    /// Cập nhật giá bán, tồn kho hoặc thông số kỹ thuật sản phẩm (Admin hoặc NhanVien)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,NhanVien")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy sản phẩm có mã ID {id}." });
        }

        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest(new { success = false, message = "Thể loại danh mục đã chọn không tồn tại." });
        }

        product.Name = request.Name.Trim();
        product.Brand = request.Brand.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;
        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            product.ImageUrl = request.ImageUrl.Trim();
        }
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Specification = request.Specification?.Trim() ?? string.Empty;
        product.CategoryId = request.CategoryId;

        await _context.SaveChangesAsync();

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Brand = product.Brand,
            Price = product.Price,
            Stock = product.Stock,
            ImageUrl = product.ImageUrl,
            Description = product.Description,
            Specification = product.Specification,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            CreatedAt = product.CreatedAt
        };

        _logger.LogInformation("Người dùng {User} đã cập nhật sản phẩm '{Name}' (ID: {Id}).", User.Identity?.Name, product.Name, product.Id);

        return Ok(new
        {
            success = true,
            message = $"Cập nhật sản phẩm '{product.Name}' thành công!",
            data = dto
        });
    }

    /// <summary>
    /// XÓA SẢN PHẨM KHỎI HỆ THỐNG - CHỈ DUY NHẤT ROLE ADMIN MỚI CÓ QUYỀN!
    /// (Nhân viên hoặc Khách hàng bấm Xóa sẽ lập tức bị chặn bằng mã 403 Forbidden)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy sản phẩm có mã ID {id}." });
        }

        // Xóa các OrderItems liên quan nếu có
        var relatedItems = await _context.OrderItems.Where(oi => oi.ProductId == id).ToListAsync();
        if (relatedItems.Any())
        {
            _context.OrderItems.RemoveRange(relatedItems);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Admin {User} đã XÓA VĨNH VIỄN sản phẩm '{Name}' (ID: {Id}) khỏi hệ thống.", User.Identity?.Name, product.Name, id);

        return Ok(new
        {
            success = true,
            message = $"Quản trị viên đã xóa thành công sản phẩm '{product.Name}' (ID: {id}) khỏi hệ thống!",
            deletedId = id
        });
    }
}
