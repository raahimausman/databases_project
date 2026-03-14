using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopRAR.Domain;

[Table("OrderItems")]
public class OrderItem
{
    [Key]
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPriceAtOrder { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotalAmount { get; set; }
    
    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
}

