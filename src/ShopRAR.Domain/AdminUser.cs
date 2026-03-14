using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopRAR.Domain;

[Table("AdminUser")]
public class AdminUser
{
    [Key]
    public int AdminUserId { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? PasswordHash { get; set; }
}

