using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ChemGa.Database.Models.Utility;

[DbSet]
[Index(nameof(UserId), IsUnique = true)]
public class GlobalBypass
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required]
    public ulong UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
