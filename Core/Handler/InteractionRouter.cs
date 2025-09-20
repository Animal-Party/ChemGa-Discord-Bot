using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ChemGa.Core.Handler;

/// <summary>
/// Dedicated router for Discord interactions (slash commands, user/context menus, components).
/// Keeps interaction-related wiring separate from legacy message command routing.
/// </summary>
public sealed class InteractionRouter : IDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider? _services;
    private bool _started;

    public InteractionRouter(DiscordSocketClient client, InteractionService? interactionService = null, IServiceProvider? services = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _interactions = interactionService ?? new InteractionService(client);
        _services = services;
    }

    public async Task RegisterModulesAsync(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        // Use helper to add nested modules as well (see Global.InteractionServiceExtensions)
        if (_services is not null)
            await _interactions.AddModulesWithNestedAsync(assembly, _services).ConfigureAwait(false);
        else
            await _interactions.AddModulesAsync(assembly, NullServiceProvider.Instance).ConfigureAwait(false);
    }

    public Task StartAsync()
    {
        if (_started) return Task.CompletedTask;

        _client.InteractionCreated += OnInteractionCreatedAsync;
        _interactions.SlashCommandExecuted += OnInteractionExecutedAsync;
        _started = true;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!_started) return Task.CompletedTask;
        _client.InteractionCreated -= OnInteractionCreatedAsync;
        _interactions.SlashCommandExecuted -= OnInteractionExecutedAsync;
        _started = false;
        return Task.CompletedTask;
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction socketInteraction)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, socketInteraction);
            await _interactions.ExecuteCommandAsync(ctx, _services ?? NullServiceProvider.Instance).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error while executing interaction");
            try
            {
                if (socketInteraction.Type == InteractionType.ApplicationCommand && socketInteraction is SocketMessageComponent comp)
                {
                    await comp.RespondAsync("An error occurred while processing this interaction.").ConfigureAwait(false);
                }
            }
            catch
            {
                // swallow; nothing we can do at this point
            }
        }
    }

    private Task OnInteractionExecutedAsync(SlashCommandInfo command, IInteractionContext context, IResult result)
    {
        try
        {
            Log.Information("Interaction {Command} executed by {User} in {Channel} with result: {Result}",
                command.Name,
                context.User.Username,
                context.Channel?.Name ?? "<dm>",
                result.IsSuccess ? "Success" : $"Error: {result.Error} - {result.ErrorReason}");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error logging executed interaction");
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
