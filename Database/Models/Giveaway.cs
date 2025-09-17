namespace ChemGa.Database.Models;


[DbSet]
public class Giveaway
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong HostId { get; set; }
    public string Prize { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int WinnerCount { get; set; }
    public ulong[] ParticipantIds { get; set; } = [];
    public bool IsEnded { get; set; } = false;
    public ulong[] RoleRestrictions { get; set; } = [];
    public ulong[] RoleRequirements { get; set; } = [];
}