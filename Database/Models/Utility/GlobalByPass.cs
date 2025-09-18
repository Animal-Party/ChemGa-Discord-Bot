using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ChemGa.Database.Models.Utility;

[DbSet]
public class GlobalBypass
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ulong UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
