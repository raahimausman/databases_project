using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopRAR.Domain;

[Table("Customer")]
public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? AddressLine { get; set; }

    [MaxLength(255)]
    public string? City { get; set; }
}
