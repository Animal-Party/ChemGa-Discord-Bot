using System.Collections.Concurrent;
using Discord.Commands;
using Serilog;
using ChemGa.Core.Common.Attributes;

namespace ChemGa.Core.Handler;

/// <summary>
/// Central cooldown store used by the router and CooldownAttribute.
/// Thread-safe, in-memory. Keyed by scope+command+entity id.
/// </summary>
[RegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton, serviceType: typeof(CooldownManager))]
public class CooldownManager
{
    private static readonly ConcurrentDictionary<string, DateTime> _expiries = new();

    private string BuildKey(CooldownScope scope, ICommandContext context, string commandName)
    {
        return scope switch
        {
            CooldownScope.Global => $"{commandName}:global",
            CooldownScope.Guild => context.Guild != null ? $"{commandName}:g:{context.Guild.Id}" : $"{commandName}:u:{context.User.Id}",
            _ => $"{commandName}:u:{context.User.Id}",
        };
    }

    /// <summary>
    /// Attempts to get remaining cooldown time. Returns true if a cooldown is active.
    /// </summary>
    public bool TryGetRemaining(CooldownScope scope, ICommandContext context, string commandName, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;
        var now = DateTime.UtcNow;
        var key = BuildKey(scope, context, commandName);
        if (_expiries.TryGetValue(key, out var expiry) && now < expiry)
        {
            remaining = expiry - now;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sets a cooldown expiry for the command.
    /// </summary>
    public void SetCooldown(CooldownScope scope, ICommandContext context, string commandName, int seconds)
    {
        if (seconds <= 0) return;
        var key = BuildKey(scope, context, commandName);
        var expiry = DateTime.UtcNow.AddSeconds(seconds);
        _expiries[key] = expiry;
    }
}
