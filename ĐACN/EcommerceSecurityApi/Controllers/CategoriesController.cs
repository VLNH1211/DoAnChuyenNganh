using EcommerceSecurityApi.Data;
using EcommerceSecurityApi.DTOs.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceSecurityApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ Thể loại Danh mục Công nghệ (Công khai - Không cần Token)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ProductCount = c.Products.Count
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Lấy chi tiết một Thể loại theo Id (Công khai)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _context.Categories
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ProductCount = c.Products.Count
            })
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy thể loại có ID {id}." });
        }

        return Ok(category);
    }
}
