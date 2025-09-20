namespace ChemGa.Core.Commands.Modules.Giveaways;
public record GiveawayStartOption(
    string Prize,
    ulong GuildId,
    ulong ChannelId,
    ulong MessageId,
    ulong HostId,
    int WinnerCount,
    TimeSpan Duration
);

public record GiveawayStartOptionWithRole(
    string Prize,
    ulong GuildId,
    ulong ChannelId,
    ulong MessageId,
    ulong HostId,
    int WinnerCount,
    TimeSpan Duration,
    ulong[] RoleIds
) : GiveawayStartOption(Prize, GuildId, ChannelId, MessageId, HostId, WinnerCount, Duration);
