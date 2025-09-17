using Discord;
using Discord.Commands;
using ChemGa.Core.Handler;

namespace ChemGa.Core.Commands;

public class HelpModule : BaseCommand
{
    [Command("help")]
    [Summary("Show list of commands or details for a specific command")]
    public async Task HelpAsync()
    {
        var metas = Context.GetAllCommandMetadata().ToList();

        var byCategory = metas.GroupBy(m => m.Category).OrderBy(g => g.Key);

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Command List")
            .WithColor(Color.Blue);

        foreach (var cat in byCategory)
        {
            var names = string.Join("\n", cat.Select(m => $"**{m.Name}** - {m.Description}"));
            embedBuilder.AddField(cat.Key, string.IsNullOrWhiteSpace(names) ? "(no commands)" : names);
        }

        await ReplyAsync(embed: embedBuilder.Build());
    }

    [Command("help")]
    [Summary("Show detailed help for a command")]
    public async Task HelpAsync([Remainder] string commandName)
    {
        var metas = Context.GetAllCommandMetadata()
            .Where(m => string.Equals(m.Name, commandName, StringComparison.OrdinalIgnoreCase) || m.Aliases.Any(a => string.Equals(a, commandName, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (metas.Count == 0)
        {
            await ReplyAsync($"No command found matching '{commandName}'.");
            return;
        }

        var m = metas.First();
        var embed = new EmbedBuilder()
            .WithTitle($"Help: {m.Name}")
            .AddField("Description", string.IsNullOrWhiteSpace(m.Description) ? "(no description)" : m.Description)
            .AddField("Aliases", m.Aliases.Length > 0 ? string.Join(", ", m.Aliases) : "None")
            .AddField("Category", m.Category)
            .AddField("Cooldown", m.CooldownSeconds)
            .WithColor(Color.Green)
            .Build();

        await ReplyAsync(embed: embed);
    }
}
