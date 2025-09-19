using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ChemGa.Database.Models.Utility;

[DbSet]
[Index(nameof(UserId), IsUnique = true)]
public class GlobalBypass
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();
    [Required]
    public ulong UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
