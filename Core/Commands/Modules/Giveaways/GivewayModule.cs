using ChemGa.Core.Common.Attributes;
using Discord.Commands;
using Discord.WebSocket;

namespace ChemGa.Core.Commands.Modules.Giveaways;

[CommandMeta]
[Cooldown(2, CooldownScope.Guild)]
[RequireBotPermission(Discord.GuildPermission.ManageMessages)]
[RequireUserPermission(Discord.GuildPermission.ManageMessages)]
[RequireRoleEx(1417541384696631356, ErrorMessage = "Bạn cần có vai trò **`{roleName}`** để sử dụng lệnh này!")]
public class GiveawayModule(DiscordSocketClient? _client, GiveawayService giveawayService) : BaseCommand(_client)
{
    [Command("giveaway-start", RunMode = RunMode.Async)]
    [Alias("gstart", "gcreate")]
    [Summary("Bắt đầu một giveaway mới.")]
    public async Task StartGiveawayAsync(string duration, int winnerCount, [Remainder] string prize)
    {
        if (!TryParseDuration(duration, out var timeSpan))
        {
            await ReplyAsync("Định dạng thời gian không hợp lệ. Vui lòng sử dụng định dạng như `10m`, `2h`, `1d`.");
            return;
        }

        if (winnerCount <= 0)
        {
            await ReplyAsync("Số lượng người thắng phải lớn hơn 0.");
            return;
        }

        if (Context.Channel is not Discord.ITextChannel channel)
        {
            await ReplyAsync("Lệnh này chỉ có thể được sử dụng trong kênh văn bản.");
            return;
        }

        var embed = new Discord.EmbedBuilder()
            .WithTitle(prize)
            .WithDescription($"**Phần thưởng:** {prize}\n**Người thắng:** {winnerCount}\n**Kết thúc vào:** {DateTime.UtcNow.Add(timeSpan).ToUniversalTime():yyyy-MM-dd HH:mm:ss} UTC")
            .WithFooter($"Bắt đầu bởi {Context.User.Username}#{Context.User.Discriminator}")
            .WithColor(Discord.Color.Green)
            .WithCurrentTimestamp();

        var message = await channel.SendMessageAsync(text: "<a:bluewing1:1417719148699713601> GIVEAWAY <a:bluewing2:1417719132807233577>", embed: embed.Build());

        await giveawayService.StartGiveawayAsync(
            prize,
            Context.Guild.Id,
            channel.Id,
            message.Id,
            Context.User.Id,
            winnerCount,
            timeSpan
        ).ConfigureAwait(false);
        
        await message.AddReactionAsync(new Discord.Emote(1418077952112988202, "a:m_heart2", true));
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
            _ => TimeSpan.Zero
        };

        return timeSpan != TimeSpan.Zero;
    }
}