using Discord.Commands;
using ChemGa.Core.Common.Attributes;

namespace ChemGa.Core.Commands;

[CommandMeta]
[Cooldown(3, CooldownScope.User)]
public class InfoModule : BaseCommand
{
    [CommandMeta]
    [Cooldown(3, CooldownScope.User)]
    [Command("ping")]
    [Alias("p")]
    [Summary("Responds with Pong")]
    public async Task PingAsync()
    {
        await ReplyAsync("Pong");
    }
}
