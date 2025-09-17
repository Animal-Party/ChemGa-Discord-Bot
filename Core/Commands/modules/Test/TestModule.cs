using ChemGa.Core.Commands.modules.Test.services;
using ChemGa.Core.Common.Attributes;
using Discord.Commands;

namespace ChemGa.Core.Commands.modules.Test;

public class TestModule(TestService testService) : BaseCommand
{
    private readonly TestService _testService = testService;

    [CommandMeta]
    [Command("test")]
    [Summary("A simple test command to demonstrate module functionality.")]
    public async Task TestCommandAsync()
    {
        var message = _testService.GetTestMessage();
        await ReplyAsync(message);
    }
}