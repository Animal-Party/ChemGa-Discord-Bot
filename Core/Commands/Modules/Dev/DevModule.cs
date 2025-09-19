using ChemGa.Core.Common.Attributes;
using ChemGa.Core.Services;
using Discord.Commands;

namespace ChemGa.Core.Commands.Modules.Dev;

[Group("dev")]
[RequireDev]
public class DevModule(IBypassService bypassService) : BaseCommand
{
    private readonly IBypassService? bypassService = bypassService;
    
    [CommandMeta]
    [Command("addbypass")]
    [Alias("ab", "addb")]
    [Summary("Add a user to the global bypass list.")]
    public async Task AddBypassAsync(ulong userId)
    {
        if (bypassService is null)
        {
            await ReplyAsync("Bypass service is not available.");
            return;
        }

        await bypassService.AddBypassAsync(userId);
        await ReplyAsync($"User with ID {userId} has been added to the global bypass list.");
    }
}