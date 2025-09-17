using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ChemGa.Core.Common.Attributes;

namespace ChemGa.Core.Handler;

/// <summary>
/// Command router implemented on top of Discord.Net's CommandService and ModuleBase.
/// Use RegisterModulesAsync to scan assemblies for modules, then StartAsync to begin handling messages.
/// </summary>
public sealed class CommandRouter(
    DiscordSocketClient client,
    CommandService? commandService = null,
    IServiceProvider? services = null,
    string? prefix = null
    ) : IDisposable
{
    private readonly DiscordSocketClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly CommandService _commands = commandService ?? new CommandService();
    private readonly string _prefix = string.IsNullOrWhiteSpace(prefix) ? "!" : prefix!;
    private readonly IServiceProvider? _services = services;
    private bool _started;

    /// <summary>
    /// Scan the provided assembly for ModuleBase implementations and add them to the CommandService.
    /// </summary>
    public async Task RegisterModulesAsync(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        await _commands.AddModulesAsync(assembly, _services ?? NullServiceProvider.Instance).ConfigureAwait(false);
    }

    /// <summary>
    /// Start listening for messages and dispatching commands. Safe to call multiple times.
    /// </summary>
    public Task StartAsync()
    {
        if (_started) return Task.CompletedTask;
        Console.WriteLine($"CommandRouter starting with prefix '{_prefix}'");
        _client.MessageReceived += OnMessageReceivedAsync;
        _commands.CommandExecuted += OnCommandExecutedAsync;
        _started = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop listening for messages.
    /// </summary>
    public Task StopAsync()
    {
        if (!_started) return Task.CompletedTask;
        _client.MessageReceived -= OnMessageReceivedAsync;
        _commands.CommandExecuted -= OnCommandExecutedAsync;
        _started = false;
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage socketMessage)
    {
        if (socketMessage is not SocketUserMessage msg) return;
        if (msg.Author.IsBot) return;

        var argPos = 0;
        if (!(msg.HasMentionPrefix(_client.CurrentUser, ref argPos) || msg.HasStringPrefix(_prefix, ref argPos)))
            return;

        var context = new SocketCommandContext(_client, msg);
        var result = await _commands.ExecuteAsync(context, argPos, _services).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            if (result.Error != CommandError.UnknownCommand)
            {
                var reason = string.IsNullOrEmpty(result.ErrorReason) ? result.Error.ToString() : result.ErrorReason;
                await context.Channel.SendMessageAsync($"Command error: {reason}").ConfigureAwait(false);
            }
        }
    }

    private Task OnCommandExecutedAsync(Optional<CommandInfo> commandOpt, ICommandContext context, IResult result)
    {
        try
        {
            if (!commandOpt.IsSpecified) return Task.CompletedTask;
            var command = commandOpt.Value;

            if (!result.IsSuccess) return Task.CompletedTask;

            var methodCooldown = command.Attributes.OfType<PreconditionAttribute>()
                .OfType<CooldownAttribute>().FirstOrDefault();

            var typeCooldown = command.Module?.GetType().GetCustomAttributes(typeof(CooldownAttribute), true)
                .OfType<CooldownAttribute>().FirstOrDefault();

            var cooldown = methodCooldown ?? typeCooldown;
            if (cooldown is not null)
            {
                CooldownManager.SetCooldown(cooldown.Scope, context, command.Name, cooldown.Seconds);
            }
        }
        catch
        { }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _ = StopAsync();
    }

    private class NullServiceProvider : IServiceProvider
    {
        public static IServiceProvider Instance { get; } = new NullServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
