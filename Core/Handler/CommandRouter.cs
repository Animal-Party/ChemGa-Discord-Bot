using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ChemGa.Core.Common.Attributes;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Discord.Rest;

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
            await HandleCommandErrorAsync(context, result).ConfigureAwait(false);
        }
    }
    private static async Task HandleCommandErrorAsync(SocketCommandContext? context, IResult result)
    {
        if (context is null) return;

        if (result.Error == CommandError.UnknownCommand) return;

        RestUserMessage? sent = null;

        if (await HandleCommandCooldownError(context, result, sent).ConfigureAwait(false))
            return;

        var reason = string.IsNullOrEmpty(result.ErrorReason) ? result.Error.ToString() : result.ErrorReason;
        try
        {
            sent = await context.Channel.SendMessageAsync($"Error: {reason}").ConfigureAwait(false);
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                    await sent.DeleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete error message");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error while handling command error message");
        }

    }
    private static async Task<bool> HandleCommandCooldownError(SocketCommandContext context, IResult result, RestUserMessage? sent)
    {
        if (result.Error == CommandError.UnknownCommand) return false;
        var reason = string.IsNullOrEmpty(result.ErrorReason) ? result.Error.ToString() : result.ErrorReason;
        try
        {
            if (string.IsNullOrEmpty(reason) || !reason.StartsWith("__COOLDOWN__:", StringComparison.OrdinalIgnoreCase)) return false;

            var parts = reason.Split(':', 2);
            if (parts.Length == 2 && int.TryParse(parts[1], out var waitSeconds) && waitSeconds > 0)
            {
                sent = await context.Channel.SendMessageAsync($"Command is on cooldown. Try again in {waitSeconds} seconds.").ConfigureAwait(false);
                var delay = TimeSpan.FromSeconds(Math.Max(1, waitSeconds));
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delay).ConfigureAwait(false);
                        await sent.DeleteAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to delete cooldown error message");
                    }
                });
            }

        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error while handling command Cooldown error message");
            return false;
        }
        return true;
    }
    private Task OnCommandExecutedAsync(Optional<CommandInfo> commandOpt, ICommandContext context, IResult result)
    {
        if (!commandOpt.IsSpecified) return Task.CompletedTask;
        try
        {
            var command = commandOpt.Value;
            var methodCooldown = command.Preconditions.OfType<CooldownAttribute>().FirstOrDefault();
            var typeCooldown = command.Module?.Preconditions.OfType<CooldownAttribute>().FirstOrDefault();

            var cooldown = typeCooldown ?? methodCooldown;
            if (cooldown is not null)
            {
                var cm = _services?.GetRequiredService<CooldownManager>();
                cm?.SetCooldown(cooldown.Scope, context, command.Name, cooldown.Seconds);
            }
        }
        finally
        {
            Log.Information("Command {Command} executed by {User} in {Channel} with result: {Result}",
                commandOpt.IsSpecified ? commandOpt.Value.Name : "<unknown>",
                context.User.Username,
                context.Channel.Name,
                result.IsSuccess ? "Success" : $"Error: {result.Error} - {result.ErrorReason}"
            );
        }

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
