namespace ShopRAR.Domain;

public class TopSellingProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQty { get; set; }
    public decimal TotalRevenue { get; set; }
}

