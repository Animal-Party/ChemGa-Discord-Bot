using ChemGa.Core.Common.Attributes;
using ChemGa.Core.Handler;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ChemGa.Core.Commands.Modules;

[CommandMeta]
[Cooldown(2, CooldownScope.User)]
public class InfoModule() : BaseCommand()
{
    [CommandMeta]
    [Command("ping")]
    [Summary("Check bot responsiveness.")]
    public async Task PingAsync()
    {
        var msg = await ReplyAsync("Pong!");
        await msg.ModifyAsync(m => m.Content = $"Pong! Latency: {msg.CreatedAt - Context.Message.CreatedAt}");
    }

    [CommandMeta]
    [Command("help")]
    [Alias("commands", "cmds")]
    [Summary("List all available commands or get detailed info about a specific command.")]
    public async Task HelpAsync([Remainder][Summary("The command to get detailed info for.")] string? commandName = null)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            // List all commands
            var categories = CommandMetadataCache.GetAll()
                .GroupBy(c => c.Category)
                .OrderBy(g => g.Key);

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Available Commands")
                .WithColor(Color.Blue)
                .WithFooter(footer => footer.Text = "Use !help <command> to get detailed info about a specific command.");

            foreach (var category in categories)
            {
                var commandList = string.Join(", ", category.Select(c => $"`{c.Name}`"));
                embedBuilder.AddField(field =>
                {
                    field.Name = $"{category.Key} ({category.Count()})";
                    field.Value = commandList;
                    field.IsInline = false;
                });
            }

            await ReplyAsync(embed: embedBuilder.Build());
        }
        else
        {
            CommandMetadataCache.TryGet(commandName, out var cmdMeta);
            if (cmdMeta is null)
            {
                await TempReplyAsync($"No command found with the name or alias '{commandName}'.");
                return;
            }

            var embedBuilder = new EmbedBuilder()
                .WithTitle($"Command: {cmdMeta.Name}")
                .WithColor(Color.Green);

            if (!string.IsNullOrWhiteSpace(cmdMeta.Description))
                embedBuilder.AddField("Description", cmdMeta.Description);

            if (cmdMeta.Aliases.Length > 0)
                embedBuilder.AddField("Aliases", string.Join(", ", cmdMeta.Aliases.Select(a => $"`{a}`")));

            if (cmdMeta.CooldownSeconds > 0)
                embedBuilder.AddField("Cooldown", $"{cmdMeta.CooldownSeconds} seconds");
            if (cmdMeta.UserPermissions?.Length > 0)
                embedBuilder.AddField("User Permissions", string.Join(", ", cmdMeta.UserPermissions.Select(p => p.ToString())));
            if (cmdMeta.BotPermissions?.Length > 0)
                embedBuilder.AddField("Bot Permissions", string.Join(", ", cmdMeta.BotPermissions.Select(p => p.ToString())));
            if (cmdMeta.RequiredRoles?.Length > 0)
                embedBuilder.AddField("Required Roles", string.Join(", ", cmdMeta.RequiredRoles.Select(r => $"`{r}`")));
                
            embedBuilder.AddField("Category", cmdMeta.Category);
            embedBuilder.WithFooter(footer => footer.Text = "Use !help to list all commands.");
            
            await ReplyAsync(embed: embedBuilder.Build());
        }
    }
}