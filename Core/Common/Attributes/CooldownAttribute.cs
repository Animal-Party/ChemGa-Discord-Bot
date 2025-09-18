using ChemGa.Core.Handler;
using Discord.Commands;
using Serilog;

namespace ChemGa.Core.Common.Attributes;

public enum CooldownScope
{
    User,
    Guild,
    Global
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class CooldownAttribute(int seconds, CooldownScope scope = CooldownScope.User) : PreconditionAttribute
{
    public int Seconds { get; } = Math.Max(0, seconds);
    public CooldownScope Scope { get; } = scope;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (Seconds <= 0) return Task.FromResult(PreconditionResult.FromSuccess());

        if (command.Attributes.Any(attr => attr is SkipCooldownAttribute))
            return Task.FromResult(PreconditionResult.FromSuccess());

        if (services is null)
            return Task.FromResult(PreconditionResult.FromSuccess());

        if (services.GetService(typeof(CooldownManager)) is not CooldownManager cooldownManager)
        {
            Log.Warning("CooldownManager service not available from IServiceProvider. Skipping cooldown check for command {Command}.", command.Name);
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        // Allow global bypass to skip cooldowns
        try
        {
            if (services?.GetService(typeof(Services.IBypassService)) is Services.IBypassService bypass)
            {
                var isBypassed = bypass.IsBypassedAsync(context.User.Id).GetAwaiter().GetResult();
                if (isBypassed) return Task.FromResult(PreconditionResult.FromSuccess());
            }
        }
        catch { }

        if (cooldownManager.TryGetRemaining(Scope, context, command.Name, out var remaining))
        {
            var remainingSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
            return Task.FromResult(PreconditionResult.FromError($"__COOLDOWN__:{remainingSeconds}"));
        }

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SkipCooldownAttribute : Attribute
{ }