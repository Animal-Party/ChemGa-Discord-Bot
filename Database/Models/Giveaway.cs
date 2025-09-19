using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace ChemGa.Database.Models;

[DbSet]
[Index(nameof(MessageId), IsUnique = true)]
[Index(nameof(EndAt), nameof(IsEnded), nameof(GuildId), AllDescending = true)]
[PrimaryKey(nameof(Id))]
public class Giveaway
{
    [Column("_id")]
    public Guid Id { get; set; } = Guid.CreateVersion7();
    [Column("guild_id")] public ulong GuildId { get; set; }
    [Required] [Column("channel_id")] public ulong ChannelId { get; set; }
    [Column("message_id")] public ulong MessageId { get; set; }
    [Required] [Column("host_id")] public ulong HostId { get; set; }
    [Required] [Column("prize")] public string Prize { get; set; } = string.Empty;
    [Column("start_at")] public DateTime StartAt { get; set; }
    [Column("end_at")] public DateTime EndAt { get; set; }
    [Column("winner_cnt")] public int WinnerCount { get; set; }
    [Column("participants")] public ulong[] ParticipantIds { get; set; } = [];
    [Column("winners")] public ulong[] WinnerIds { get; set; } = [];
    [Column("ended")] public bool IsEnded { get; set; } = false;
    /// <summary>
    /// Roles that are NOT allowed to participate.
    /// </summary>
    [Column("roles_restrict")] public ulong[] RoleRestrictions { get; set; } = [];

    /// <summary>
    /// Roles required to participate. If not empty, a participant must have at least one of these roles.
    /// </summary>
    [Column("roles_required")] public ulong[] RoleRequirements { get; set; } = System.Array.Empty<ulong>();
}