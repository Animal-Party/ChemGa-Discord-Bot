using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using Serilog;

namespace ChemGa.Core.Handler;

/// <summary>
/// A simple collector for message component interactions (buttons / select menus).
/// Works similarly to discord.js collectors: listens for interactions for a given message id,
/// applies an optional predicate, and completes when a timeout elapses or a max count is reached.
/// </summary>
public sealed class ComponentInteractionCollector(
    DiscordSocketClient client,
    ulong messageId,
    TimeSpan timeout,
    Func<SocketMessageComponent, bool>? filter = null,
    int? max = null
) : IDisposable
{
    private readonly DiscordSocketClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ulong _messageId = messageId;
    private readonly Func<SocketMessageComponent, bool>? _filter = filter;
    private readonly TimeSpan _timeout = timeout;
    private readonly int? _max = max;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<SocketMessageComponent> _collected = new();
    private TaskCompletionSource<SocketMessageComponent[]?>? _tcs;
    /// <summary>
    /// Invoked for each component as soon as it is collected.
    /// Handlers should be async and will be invoked in the background.
    /// </summary>
    public event Func<SocketMessageComponent, Task>? OnReceived;

    /// <summary>
    /// Reason the collector ended.
    /// </summary>
    public enum EndReason { Timeout, MaxReached, Stopped }

    /// <summary>
    /// Fired when the collector ends. Provides the collected items and the reason.
    /// </summary>
    public event Func<SocketMessageComponent[]?, EndReason, Task>? OnEnded;

    /// <summary>
    /// Start collecting. Returns when timed out or max collected.
    /// </summary>
    public Task<SocketMessageComponent[]?> CollectAsync()
    {
        if (_tcs != null) throw new InvalidOperationException("Collector already started");
        _tcs = new TaskCompletionSource<SocketMessageComponent[]?>(TaskCreationOptions.RunContinuationsAsynchronously);

        _client.InteractionCreated += OnInteractionCreatedAsync;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_timeout, _cts.Token).ConfigureAwait(false);
                Finish();
            }
            catch (OperationCanceledException) { /* canceled because completed early */ }
        });

        return _tcs.Task;
    }

    private Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        if (interaction is not SocketMessageComponent comp) return Task.CompletedTask;
        try
        {
            if (comp.Message == null) return Task.CompletedTask;
            if (comp.Message.Id != _messageId) return Task.CompletedTask;

            if (_filter != null && !_filter(comp)) return Task.CompletedTask;

            _collected.Enqueue(comp);

            // fire per-item handlers (don't await)
            var handler = OnReceived;
            if (handler != null)
            {
                try
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await handler(comp).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Error in OnReceived handler");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to invoke OnReceived");
                }
            }

            if (_max.HasValue && _collected.Count >= _max.Value)
            {
                Finish();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Collector error");
        }

        return Task.CompletedTask;
    }

    private void Finish(EndReason reason = EndReason.Timeout)
    {
        try
        {
            _client.InteractionCreated -= OnInteractionCreatedAsync;
            _cts.Cancel();
            var arr = _collected.ToArray();
            _tcs?.TrySetResult(arr);

            var ended = OnEnded;
            if (ended != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ended(arr, reason).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error in OnEnded handler");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _tcs?.TrySetException(ex);
        }
    }

    /// <summary>
    /// Stop the collector early and fire OnEnded with reason Stopped.
    /// </summary>
    public void Stop()
    {
        try
        {
            Finish(EndReason.Stopped);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error stopping collector");
        }
    }

    public void Dispose()
    {
        try
        {
            _client.InteractionCreated -= OnInteractionCreatedAsync;
            _cts.Cancel();
            _cts.Dispose();
        }
        catch { }
    }
}
