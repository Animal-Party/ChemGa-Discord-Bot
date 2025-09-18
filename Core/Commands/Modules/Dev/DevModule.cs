using ChemGa.Core.Common.Attributes;
using ChemGa.Core.Services;
using Discord.Commands;
using Discord.WebSocket;

namespace ChemGa.Core.Commands.Modules.Dev;

[Group("dev")]
[RequireDev]
public class DevModule(DiscordSocketClient? client) : BaseCommand(client)
{
    [CommandMeta]
    [Command("test")]
    [Summary("Check bot latency.")]
    public async Task PingAsync()
    {
        var latency = Context.Client.Latency;
        await ReplyAsync($"Pong! Latency: {latency}ms");
    }

    [CommandMeta]
    [Command("addbypass")]
    [Alias("ab", "addb")]
    [Summary("Add a user to the global bypass list.")]
    public async Task AddBypassAsync(ulong userId, IBypassService bypassService)
    {
        await bypassService.AddBypassAsync(userId);
        await ReplyAsync($"User with ID {userId} has been added to the global bypass list.");
    }
}