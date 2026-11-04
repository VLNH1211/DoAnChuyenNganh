using System.ComponentModel.DataAnnotations;

namespace EcommerceSecurityApi.DTOs.Orders;

public class UpdateOrderStatusDto
{
    [Required]
    [RegularExpression("^(Pending|Processing|Completed|Cancelled)$", ErrorMessage = "Trạng thái phải là: Pending, Processing, Completed hoặc Cancelled")]
    public string Status { get; set; } = string.Empty;
}

public class OrderStatsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalProductsSold { get; set; }
    public int TotalComicsSold => TotalProductsSold; // Backward compatibility
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
}
