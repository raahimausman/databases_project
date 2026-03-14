using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopRAR.Domain;

[Table("Inventory")]
public class Inventory
{
    [Key]
    public int ProductId { get; set; }

    public int QuantityOnHand { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
}

