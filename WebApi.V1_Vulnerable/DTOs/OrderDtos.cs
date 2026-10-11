namespace WebApi.V1_Vulnerable.DTOs
{
    /// <summary>
    /// V1: Cho phép client tự do gán UserId, Status, TotalAmount bất kỳ (Lỗ hổng Mass Assignment & Business Logic Flaw)
    /// </summary>
    public class CreateOrderRequestV1
    {
        public int? UserId { get; set; }
        public string? CustomerName { get; set; }
        public string? ProductName { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// V1: Cập nhật đơn hàng không kiểm tra chặt chẽ
    /// </summary>
    public class UpdateOrderRequestV1
    {
        public string? ProductName { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
    }
}
