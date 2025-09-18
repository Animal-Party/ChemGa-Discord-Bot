using Microsoft.EntityFrameworkCore;
using ChemGa.Database.Utils;
using System.ComponentModel.DataAnnotations;

namespace ChemGa.Database.Models;


[DbSet]
[Index(nameof(MessageId), IsUnique = true)]
[PrimaryKey(nameof(Id))]
public class Giveaway
{
    [MaxLength(36)]
    public string Id { get; set; } = UuidV7.NewString();
    public ulong GuildId { get; set; }
    [Required] public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    [Required] public ulong HostId { get; set; }
    [Required] public string Prize { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int WinnerCount { get; set; }
    public ulong[] ParticipantIds { get; set; } = [];
    public bool IsEnded { get; set; } = false;
    public ulong[] RoleRestrictions { get; set; } = [];
    public ulong[] RoleRequirements { get; set; } = [];
}