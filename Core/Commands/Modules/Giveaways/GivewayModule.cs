using ChemGa.Core.Common.Attributes;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ChemGa.Core.Commands.Modules.Giveaways;

[CommandMeta]
[Cooldown(2, CooldownScope.Guild)]
[RequireBotPermissionEx(GuildPermission.ManageMessages)]
[RequireUserPermissionEx(GuildPermission.ManageMessages)]
[RequireRoleEx(1417541384696631356, ErrorMessage = "Bạn cần có vai trò **`{roleName}`** để sử dụng lệnh này!")]
public class GiveawayModule(DiscordSocketClient? _client, GiveawayService giveawayService) : BaseCommand(_client)
{
    private static readonly int MAX_GIVEAWAYS_PER_GUILD = 20;

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