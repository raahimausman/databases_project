using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopRAR.Domain;

[Table("Review")]
public class Review
{
    [Key]
    public int ReviewId { get; set; }

    public int ProductId { get; set; }

    public int CustomerId { get; set; }

    public int Rating { get; set; }

    public string? Comments { get; set; }

    [MaxLength(10)]
    public string? IsApproved { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("CustomerId")]
    public Customer? Customer { get; set; }
}

