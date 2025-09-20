using ChemGa.Core.Common.Attributes;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ChemGa.Core.Commands.Modules.Giveaways;

[CommandMeta]
[Cooldown(2, CooldownScope.Guild)]
[RequireBotPermissionEx(GuildPermission.ManageMessages)]
[RequireUserPermissionEx(GuildPermission.ManageMessages)]
public class GiveawayModule(GiveawayService giveawayService) : BaseCommand()
{
    private static readonly int MAX_GIVEAWAYS_PER_GUILD = 20;

    [CommandMeta]
    [Command("giveaway-start", RunMode = RunMode.Async)]
    [Alias("gstart", "gcreate", "ga")]
    [Summary("Bắt đầu một giveaway mới.")]
    [SkipCooldown]
    public async Task StartGiveawayAsync(string duration, int winnerCount, [Remainder] string prize)
    {
        if (!TryParseDuration(duration, out var timeSpan))
        {
            await TempReplyAsync("Định dạng thời gian không hợp lệ. Vui lòng sử dụng định dạng như `10m`, `2h`, `1d`.");
            return;
        }

        if (timeSpan < TimeSpan.FromSeconds(10) || timeSpan > TimeSpan.FromDays(7))
        {
            await TempReplyAsync("Thời lượng giveaway phải từ 10 giây đến 7 ngày.");
            return;
        }

        if (winnerCount <= 0)
        {
            await TempReplyAsync("Số lượng người thắng phải lớn hơn 0.");
            return;
        }

        if (Context.Channel is not ITextChannel channel)
        {
            await TempReplyAsync("Lệnh này chỉ có thể được sử dụng trong kênh văn bản.");
            return;
        }

        _ = Context.Message.DeleteAsync().ConfigureAwait(false);

        var existingGiveaways = giveawayService.GetActiveGiveaways(Context.Guild.Id).Count();
        if (existingGiveaways >= MAX_GIVEAWAYS_PER_GUILD)
        {
            await TempReplyAsync($"Máy chủ của bạn đã đạt đến giới hạn tối đa là **`{MAX_GIVEAWAYS_PER_GUILD}`** giveaway đang hoạt động. Vui lòng kết thúc một giveaway hiện tại trước khi tạo mới.");
            return;
        }

        await giveawayService.PublishGiveawayMessageAsync(
            Context.Message,
            new GiveawayStartOption(prize, Context.Guild.Id, Context.Channel.Id, Context.Message.Id, Context.User.Id, winnerCount, timeSpan)
        ).ConfigureAwait(false);
    }

    [CommandMeta]
    [Command("giveaway-reroll", RunMode = RunMode.Async)]
    [Alias("greroll", "gr")]
    [Summary("Chọn lại người thắng cho một giveaway đã kết thúc.")]
    public async Task RerollGiveawayAsync(ulong messageId)
    {
        var giveaway = giveawayService.GetGiveaway(messageId);
        if (giveaway is null)
        {
            await TempReplyAsync("Không tìm thấy giveaway với ID tin nhắn đã cho.");
            return;
        }

        if (!giveaway.IsEnded)
        {
            await TempReplyAsync("Giveaway này vẫn đang hoạt động. Bạn chỉ có thể chọn lại người thắng cho các giveaway đã kết thúc.");
            return;
        }

        var newWinners = await giveawayService.RerollGiveawayAsync(messageId).ConfigureAwait(false);
        if (newWinners == null || newWinners.Length == 0)
        {
            await TempReplyAsync("Không thể chọn người thắng mới. Có thể không có đủ người tham gia.");
            return;
        }
        await Context.Message.DeleteAsync().ConfigureAwait(false);
    }

    [CommandMeta]
    [Command("giveaway-end", RunMode = RunMode.Async)]
    [Alias("gend", "ge")]
    [Summary("Kết thúc một giveaway đang hoạt động ngay lập tức.")]
    public async Task EndGiveawayAsync(ulong messageId)
    {
        var giveaway = giveawayService.GetGiveaway(messageId);
        if (giveaway is null)
        {
            await TempReplyAsync("Không tìm thấy giveaway với ID tin nhắn đã cho.");
            return;
        }

        if (giveaway.IsEnded)
        {
            await TempReplyAsync("Giveaway này đã kết thúc.");
            return;
        }

        var _ = Context.Message.DeleteAsync().ConfigureAwait(false);

        await giveawayService.EndGiveawayAsync(giveaway).ConfigureAwait(false);
    }


    [CommandMeta]
    [Command("giveaway-list", RunMode = RunMode.Async)]
    [Alias("gl", "glist")]
    [Summary("Liệt kê các giveaway đang hoạt động trong máy chủ.")]
    public async Task ListGiveawaysAsync()
    {
        var giveaways = giveawayService.GetActiveGiveaways(Context.Guild.Id).ToList();
        if (giveaways.Count == 0)
        {
            await TempReplyAsync("Không có giveaway nào đang hoạt động trong máy chủ này.");
            return;
        }

        var embedBuilder = Context.Client.Embed()
            .WithTitle("Giveaways Đang Hoạt Động")
            .WithColor(Color.Purple)
            .WithFooter($"Yêu cầu bởi {Context.User.Username}", Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
            .WithCurrentTimestamp();

        foreach (var giveaway in giveaways)
        {
            var timeRemaining = new DateTimeOffset(giveaway.EndAt).ToUnixTimeSeconds();
            var channel = Context.Guild.GetTextChannel(giveaway.ChannelId);
            var channelMention = channel != null ? channel.Mention : $"Kênh ID: {giveaway.ChannelId}";

            embedBuilder.AddField(
                $"{giveaway.Prize} - {giveaway.WinnerCount} người thắng",
                $"Kênh: {channelMention}\nKết thúc trong: <t:{timeRemaining}:R>\nID ga: `{giveaway.MessageId}`"
            );
        }

        await ReplyAsync(embed: embedBuilder.Build());
    }

    [CommandMeta]
    [Command("giveaway-start-role", RunMode = RunMode.Async)]
    [Alias("gstartrole", "gcreaterole", "garole")]
    [Summary("Bắt đầu một giveaway mới với vai trò yêu cầu.")]
    public async Task StartGiveawayWithRoleAsync(string duration, int winnerCount, [Remainder] string prize)
    {
        if (!TryParseDuration(duration, out var timeSpan))
        {
            await TempReplyAsync("Định dạng thời gian không hợp lệ. Vui lòng sử dụng định dạng như `10m`, `2h`, `1d`.");
            return;
        }

        if (timeSpan < TimeSpan.FromSeconds(10) || timeSpan > TimeSpan.FromDays(7))
        {
            await TempReplyAsync("Thời lượng giveaway phải từ 10 giây đến 7 ngày.");
            return;
        }

        if (winnerCount <= 0)
        {
            await TempReplyAsync("Số lượng người thắng phải lớn hơn 0.");
            return;
        }

        if (Context.Channel is not ITextChannel channel)
        {
            await TempReplyAsync("Lệnh này chỉ có thể được sử dụng trong kênh văn bản.");
            return;
        }

        _ = Context.Message.DeleteAsync().ConfigureAwait(false);

        var existingGiveaways = giveawayService.GetActiveGiveaways(Context.Guild.Id).Count();
        if (existingGiveaways >= MAX_GIVEAWAYS_PER_GUILD)
        {
            await TempReplyAsync($"Máy chủ của bạn đã đạt đến giới hạn tối đa là **`{MAX_GIVEAWAYS_PER_GUILD}`** giveaway đang hoạt động. Vui lòng kết thúc một giveaway hiện tại trước khi tạo mới.");
            return;
        }

        var embed = Context.Client.Embed()
            .WithTitle("Chọn Vai Trò Yêu Cầu")
            .WithDescription("Vui lòng phản ứng với tin nhắn này bằng vai trò bạn muốn yêu cầu để tham gia giveaway.\n\nNếu bạn không phản ứng trong vòng 2 phút, giveaway sẽ bị hủy.")
            .WithColor(Color.Orange)
            .WithFooter($"Yêu cầu bởi {Context.User.Username}", Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
            .WithCurrentTimestamp();

        var menuRoles = new ComponentBuilder()
            .WithSelectMenu(new SelectMenuBuilder()
                .WithCustomId($"giveaway:role:select:{Context.Message.Id}")
                .WithPlaceholder("> Chọn vai trò (tối đa 5)")
                .WithType(ComponentType.RoleSelect)
            )
            .WithButton("Hủy", $"giveaway:role:select:cancel:{Context.Message.Id}", ButtonStyle.Danger);

        var promptMessage = await channel.SendMessageAsync(embed: embed.Build(), components: menuRoles.Build()).ConfigureAwait(false);

        using var collector = new Handler.ComponentInteractionCollector(
            Context.Client,
            promptMessage.Id,
            TimeSpan.FromMinutes(1),
            comp =>
                comp.Data.CustomId.StartsWith($"giveaway:role:") && comp.User.Id == Context.User.Id,
            max: 1
        );

        collector.OnReceived += async comp =>
        {
            try
            {
                if (comp.Data.CustomId.Contains("cancel"))
                {
                    await promptMessage.DeleteAsync().ConfigureAwait(false);
                    await comp.RespondAsync("Giveaway đã bị hủy.", ephemeral: true).ConfigureAwait(false);
                    return;
                }

                await comp.DeferAsync(ephemeral: true).ConfigureAwait(false);

                var selectedValues = comp.Data.Values.ToArray();
                if (selectedValues.Length == 0)
                {
                    await comp.FollowupAsync("Bạn phải chọn ít nhất một vai trò để tiếp tục.", ephemeral: true).ConfigureAwait(false);
                    return;
                }

                if (selectedValues.Length > 5)
                {
                    await comp.FollowupAsync("Bạn chỉ có thể chọn tối đa 5 vai trò.", ephemeral: true).ConfigureAwait(false);
                    return;
                }

                var roles = selectedValues
                    .Select(id => Context.Guild.GetRole(ulong.Parse(id)))
                    .Where(role => role != null)
                    .ToList();

                if (roles.Count == 0)
                {
                    await comp.FollowupAsync("Không tìm thấy vai trò hợp lệ nào từ lựa chọn của bạn.", ephemeral: true).ConfigureAwait(false);
                    return;
                }

                var giveawayOption = new GiveawayStartOptionWithRole(
                    Prize: prize,
                    GuildId: Context.Guild.Id,
                    ChannelId: Context.Channel.Id,
                    MessageId: promptMessage.Id,
                    HostId: Context.User.Id,
                    WinnerCount: winnerCount,
                    Duration: timeSpan,
                    RoleIds: roles.Select(r => r!.Id).ToArray()
                );

                await giveawayService.PublishGiveawayMessageAsync(promptMessage, giveawayOption).ConfigureAwait(false);
                await promptMessage.DeleteAsync().ConfigureAwait(false);
                await comp.FollowupAsync("Giveaway đã được tạo thành công với vai trò yêu cầu.", ephemeral: true).ConfigureAwait(false);
            }
            catch {}
        };

        collector.OnEnded += async (collected, reason) =>
        {
            await promptMessage.DeleteAsync().ConfigureAwait(false);
        };


        await collector.CollectAsync().ConfigureAwait(false);
    }

    private static bool TryParseDuration(string duration, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(duration) || duration.Length < 2)
            return false;

        var timePart = duration[..^1];
        var unit = duration[^1];

        if (!int.TryParse(timePart, out var timeValue) || timeValue <= 0)
            return false;

        timeSpan = unit switch
        {
            'm' => TimeSpan.FromMinutes(timeValue),
            'h' => TimeSpan.FromHours(timeValue),
            'd' => TimeSpan.FromDays(timeValue),
            's' => TimeSpan.FromSeconds(timeValue),
            _ => TimeSpan.Zero
        };

        return timeSpan != TimeSpan.Zero;
    }
}