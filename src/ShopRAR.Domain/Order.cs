using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopRAR.Domain;

[Table("Orders")]
public class Order
{
    [Key]
    public int OrderId { get; set; }

    public DateTime? OrderDate { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAmount { get; set; }

    public int? CustomerId { get; set; }

    public int? AdminUserId { get; set; }

    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }
}

