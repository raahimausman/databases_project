namespace ShopRAR.Domain;

public class CustomerLifetimeSpend
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public decimal LifetimeSpend { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

