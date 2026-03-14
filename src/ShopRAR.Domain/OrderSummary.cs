namespace ShopRAR.Domain;

public class OrderSummary
{
    public int OrderId { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? Status { get; set; }
    public decimal? StoredTotalAmount { get; set; }
    public decimal CalculatedTotalAmount { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public int? AdminUserId { get; set; }
    public string? AdminEmail { get; set; }
}

