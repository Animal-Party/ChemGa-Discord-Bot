using ChemGa.Core.Common.Attributes;
using ChemGa.Database;
using ChemGa.Database.Models;
using ChemGa.Interfaces;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ChemGa.Core.Common.Utils;

namespace ChemGa.Core.Commands.Modules.Giveaways;

[RegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, serviceType: typeof(GiveawayService))]
public class GiveawayService(AppDatabase db, DiscordSocketClient _client, IDbWriteLocker dbWriteLocker) : IService
{
    private readonly AppDatabase _db = db;
    private readonly DiscordSocketClient _client = _client;
    private readonly IDbWriteLocker _dbWriteLocker = dbWriteLocker;
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private readonly DbSet<Giveaway> _giveaways = db.GetModel<Giveaway>();
    /// <summary>
    /// Lấy danh sách giveaway còn hoạt động (chưa hết hạn).
    /// </summary>
    public IQueryable<Giveaway> GetActiveGiveaways(ulong? guildId = null)
    {
        return _giveaways
            .AsNoTracking()
            .Where(g => g.EndAt > DateTime.UtcNow && !g.IsEnded && (guildId == null || g.GuildId == guildId));
    }

    /// <summary>
    /// Lấy danh sách giveaway đã hết hạn nhưng chưa xử lý.
    /// </summary>
    public IQueryable<Giveaway> GetEndedGiveaways()
    {
        return _giveaways
            .AsNoTracking()
            .Where(g => g.EndAt <= DateTime.UtcNow && !g.IsEnded)
            .Distinct();
    }
    public Giveaway? GetGiveaway(ulong messageId)
    {
        return _giveaways
            .AsNoTracking()
            .FirstOrDefault(g => g.MessageId == messageId);
    }
    public Task<bool> GiveawayExistsAsync(ulong messageId)
    {
        return _giveaways.AnyAsync(g => g.MessageId == messageId);
    }

    /// <summary>
    /// Bắt đầu một giveaway mới và lưu vào database.
    /// </summary>
    public async Task<Giveaway> StartGiveawayAsync(GiveawayStartOption option)
    {
        var (prize, guildId, channelId, messageId, hostId, winnerCount, duration) = option;

        var giveaway = new Giveaway
        {
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.Add(duration),
            Prize = prize,
            GuildId = guildId,
            ChannelId = channelId,
            MessageId = messageId,
            HostId = hostId,
            WinnerCount = winnerCount,
            IsEnded = false
        };

        await _dbWriteLocker.RunAsync(async scopedDb =>
        {
            var set = scopedDb.GetModel<Giveaway>();
            await set.AddAsync(giveaway).ConfigureAwait(false);
            await scopedDb.SaveChangesAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        return giveaway;
    }

    public async Task<Giveaway> StartGiveawayAsync(GiveawayStartOptionWithRole option)
    {
        var (prize, guildId, channelId, messageId, hostId, winnerCount, duration, listRoles) = option;

        var giveaway = new Giveaway
        {
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.Add(duration),
            Prize = prize,
            GuildId = guildId,
            ChannelId = channelId,
            MessageId = messageId,
            HostId = hostId,
            WinnerCount = winnerCount,
            IsEnded = false,
            RoleRequirements = [.. listRoles]
        };

        await _dbWriteLocker.RunAsync(async scopedDb =>
        {
            var set = scopedDb.GetModel<Giveaway>();
            await set.AddAsync(giveaway).ConfigureAwait(false);
            await scopedDb.SaveChangesAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        return giveaway;
    }

    /// <summary>
    /// Đánh dấu giveaway đã kết thúc.
    /// </summary>
    public async Task EndGiveawayAsync(Giveaway giveaway, bool isReroll = false)
    {
        IMessage? message = null;
        try
        {
            if (await _client.Rest.GetChannelAsync(giveaway.ChannelId) is IMessageChannel channel)
            {
                message = await channel.GetMessageAsync(giveaway.MessageId).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to fetch channel/message for giveaway {GiveawayId}", giveaway.Id);
        }

        if (message == null && !isReroll)
        {
            Log.Warning("Giveaway message not found for giveaway {GiveawayId}, skipping end process.", giveaway.Id);
            return;
        }

        IEmote? giveawayEmote = null;
        if (message != null)
        {
            foreach (var kv in message.Reactions)
            {
                if (kv.Key is Emote e)
                {
                    if (e.Id == 1418077952112988202UL)
                    {
                        giveawayEmote = e;
                        break;
                    }
                }
            }
        }


        var participants = new List<ulong>();

        if (giveawayEmote != null && message != null && !isReroll)
        {
            var pageCounter = 0;
            var rng = new Random();
            await foreach (var page in message.GetReactionUsersAsync(giveawayEmote, 30))
            {
                if (page == null || page.Count == 0) continue;
                foreach (var user in page)
                {
                    if (user.IsBot) continue;
                    if (message.Channel is ITextChannel textChannel)
                    {
                        var guild = textChannel.Guild;
                        var guildUser = await guild.GetUserAsync(user.Id);
                        if (IsValidParticipant(guildUser, giveaway))
                        {
                            participants.Add(user.Id);
                        }
                    }
                }

                pageCounter++;
                if (pageCounter % 5 == 0)
                {
                    await Task.Delay(1000 + rng.Next(0, 500));
                }
            }
        }
        else if (isReroll)
        {
            if (giveaway.ParticipantIds != null && giveaway.ParticipantIds.Length > 0)
            {
                participants.AddRange(giveaway.ParticipantIds);
            }
        }

        // If we could not fetch reaction users (e.g., message missing), fall back to stored participant ids
        if (participants.Count == 0 && (message == null || giveawayEmote == null))
        {
            if (giveaway.ParticipantIds != null && giveaway.ParticipantIds.Length > 0)
            {
                participants.AddRange(giveaway.ParticipantIds);
            }
        }

        giveaway.ParticipantIds = [.. participants.Distinct()];

        var winners = DrawWinnersFromList(giveaway.ParticipantIds, giveaway.WinnerCount);
        giveaway.WinnerIds = [.. winners.Distinct()];

        await _dbWriteLocker.RunAsync(async scopedDb =>
        {
            var set = scopedDb.GetModel<Giveaway>();
            set.Update(giveaway);
            await scopedDb.SaveChangesAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        giveaway.IsEnded = true;
        await _dbWriteLocker.RunAsync(async scopedDb =>
        {
            var set = scopedDb.GetModel<Giveaway>();
            set.Update(giveaway);
            await scopedDb.SaveChangesAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        try
        {
            IGuildUser? hostUser = null;
            try
            {
                hostUser = _client.GetGuild(giveaway.GuildId)?.GetUser(giveaway.HostId);
                if (hostUser == null)
                {
                    var guild = await _client.Rest.GetGuildAsync(giveaway.GuildId).ConfigureAwait(false);
                    hostUser = await guild.GetUserAsync(giveaway.HostId);
                }
            }
            catch { }

            var embed = BuildGiveawayEmbed(
                    giveaway,
                    hostUser ?? throw new InvalidOperationException("Host not available"),
                    ended: true)
                .WithCurrentTimestamp()
                .WithFooter(new EmbedFooterBuilder { Text = $"Đã kết thúc" });

            if (winners.Length == 0)
            {
                embed.WithDescription("Buồn qué ಥ_ಥ ~ Không ai tham gia giveaway này cả.");
            }

            if (message is IUserMessage um)
            {
                await um.ModifyAsync(props =>
                {
                    props.Content = "# <a:bling:1418195302053187584><a:bluewing1:1417719148699713601> GIVEAWAY END <a:bluewing2:1417719132807233577><a:bling:1418195302053187584>";
                    props.Embed = embed.Build();
                }).ConfigureAwait(false);
            }

            if (message?.Channel is not null && winners.Length > 0)
            {
                var messageUrl = $"https://discord.com/channels/{giveaway.GuildId}/{giveaway.ChannelId}/{giveaway.MessageId}";
                var winMessage = $"### <:lathu:1417749932982665269> | Xin chúc mừng {string.Join(", ", winners.Select(w => $"<@{w}>"))} đã trúng giveaway __{giveaway.Prize}__ tổ chức bởi <@{giveaway.HostId}>!\n";
                await message.Channel.SendMessageAsync(
                    text: winMessage,
                    messageReference: null,
                    components: new ComponentBuilder().WithButton("Giveway", style: ButtonStyle.Link, url: messageUrl).Build()
                ).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to announce giveaway end for {GiveawayId}", giveaway.Id);
        }
    }


    private static bool IsValidParticipant(IGuildUser? user, Giveaway giveaway)
    {
        if (user == null) return false;
        if (user.IsBot) return false;

        if (giveaway.RoleRestrictions != null && giveaway.RoleRestrictions.Length > 0)
        {
            if (user.RoleIds.Any(rid => giveaway.RoleRestrictions.Contains(rid)))
                return false;
        }

        if (giveaway.RoleRequirements != null && giveaway.RoleRequirements.Length > 0)
        {
            if (!user.RoleIds.Any(rid => giveaway.RoleRequirements.Contains(rid)))
                return false;
        }

        return true;
    }

    private static ulong[] DrawWinnersFromList(ulong[] pool, int count)
    {
        if (pool == null || pool.Length == 0 || count <= 0) return [];
        var list = pool.Distinct().ToList();
        if (list.Count <= count) return [.. list];

        var rng = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[j], list[i]) = (list[i], list[j]);
        }

        return [.. list.Take(count)];
    }

    public EmbedBuilder BuildGiveawayEmbed(Giveaway giveaway, IGuildUser host, bool ended = false)
    {
        const string header = "<a:cg_lin:1417870397415755828> *nhấn vào <a:cg_hblue:1418077952112988202> để tham gia giveaways*";
        const string heart = "<:cg_neheart:1418077490144088096>";

        var endUnix = new DateTimeOffset(giveaway.EndAt).ToUnixTimeSeconds();

        var winnerIds = giveaway.WinnerIds ?? [];
        var roleReqs = giveaway.RoleRequirements ?? [];

        var description = $"""
    {(ended ? "" : header)}
    {(ended ? $"{heart} Người chiến thắng: {string.Join(" ", winnerIds.Select(u => $"<@{u}>"))}" : $"{heart} Kết thúc trong: <t:{endUnix}:R>")} 
    {heart} Tổ chức bởi: <@{giveaway.HostId}>

    {(roleReqs.Length > 0 ? $"{heart} Yêu cầu vai trò: {string.Join(", ", roleReqs.Select(rid => $"<@&{rid}>"))}\n" : "")}
    """.Trim();

        var embed = _client.Embed()
            .WithTitle(giveaway.Prize)
            .WithDescription(description)
            .WithAuthor(host.Guild.Name, host.Guild.IconUrl ?? _client.CurrentUser.GetAvatarUrl())
            .WithThumbnailUrl(host.GetAvatarUrl() ?? _client.CurrentUser.GetAvatarUrl())
            .WithFooter($"Giveaway với {giveaway.WinnerCount} giải", _client.CurrentUser.GetAvatarUrl())
            .WithCurrentTimestamp();

        return embed;
    }

    public async Task PublishGiveawayMessageAsync(IUserMessage message, GiveawayStartOption option)
    {
        var giveaway = await StartGiveawayAsync(option).ConfigureAwait(false);
        var host = _client.GetGuild(giveaway.GuildId).GetUser(giveaway.HostId);

        var embedBuilder = BuildGiveawayEmbed(giveaway, host, ended: false);

        IMessage? newMessage = null;
        if (message.Channel is IMessageChannel ch)
        {
            try
            {
                newMessage = await ch.SendMessageAsync(
                    text: "# <a:bling:1418195302053187584><a:bluewing1:1417719148699713601> GIVEAWAY <a:bluewing2:1417719132807233577><a:bling:1418195302053187584>",
                    embed: embedBuilder.Build()
                ).ConfigureAwait(false);
            }
            catch { /* ignore send failures */ }
        }

        try
        {
            if (newMessage is IUserMessage newUm)
            {
                try
                {
                    var emote = Emote.Parse("<a:cg_hblue:1418077952112988202>");
                    await newUm.AddReactionAsync(emote).ConfigureAwait(false);
                }
                catch
                {
                }

                giveaway.MessageId = newUm.Id;
                await _dbWriteLocker.RunAsync(async scopedDb =>
                {
                    var set = scopedDb.GetModel<Giveaway>();
                    set.Update(giveaway);
                    await scopedDb.SaveChangesAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }
        catch { /* ignore reaction failures */ }
    }
    public async Task PublishGiveawayMessageAsync(IUserMessage message, GiveawayStartOptionWithRole option)
    {
        var giveaway = await StartGiveawayAsync(option).ConfigureAwait(false);
        var host = _client.GetGuild(giveaway.GuildId).GetUser(giveaway.HostId);

        var embedBuilder = BuildGiveawayEmbed(giveaway, host, ended: false);

        IMessage? newMessage = null;
        if (message.Channel is IMessageChannel ch)
        {
            try
            {
                newMessage = await ch.SendMessageAsync(
                    text: "# <a:bling:1418195302053187584><a:bluewing1:1417719148699713601> GIVEAWAY <a:bluewing2:1417719132807233577><a:bling:1418195302053187584>",
                    embed: embedBuilder.Build()
                ).ConfigureAwait(false);
            }
            catch { /* ignore send failures */ }
        }

        try
        {
            if (newMessage is IUserMessage newUm)
            {
                try
                {
                    var emote = Emote.Parse("<a:cg_hblue:1418077952112988202>");
                    await newUm.AddReactionAsync(emote).ConfigureAwait(false);
                }
                catch
                {
                }

                giveaway.MessageId = newUm.Id;
                await _dbWriteLocker.RunAsync(async scopedDb =>
                {
                    var set = scopedDb.GetModel<Giveaway>();
                    set.Update(giveaway);
                    await scopedDb.SaveChangesAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }
        catch { /* ignore reaction failures */ }
    }

    private async Task ReCheckGiveaway(CancellationToken token)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        const int concurrency = 3;
        while (await timer.WaitForNextTickAsync(token))
        {
            var endedGiveaways = GetEndedGiveaways()
                .OrderBy(g => g.EndAt)
                .Take(20)
                .ToList();

            if (endedGiveaways.Count == 0) continue;

            var semaphore = new SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            foreach (var giveaway in endedGiveaways)
            {
                await semaphore.WaitAsync(token).ConfigureAwait(false);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {

                        await EndGiveawayAsync(giveaway).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error processing ended giveaway {GiveawayId}", giveaway.Id);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, token));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
    }

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        _worker = ReCheckGiveaway(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_worker != null)
            {
                await _worker;
            }
            _cts.Dispose();
            _cts = null;
        }

        await _dbWriteLocker.RunAsync(scopedDb => scopedDb.SaveChangesAsync()).ConfigureAwait(false);
    }

    internal async Task<ulong[]> RerollGiveawayAsync(ulong messageId)
    {
        var giveaway = GetGiveaway(messageId);
        if (giveaway is null)
            return [];
        await EndGiveawayAsync(giveaway, isReroll: true).ConfigureAwait(false);

        var updated = GetGiveaway(messageId);
        return updated?.WinnerIds ?? [];
    }
}