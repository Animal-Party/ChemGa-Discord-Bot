using ChemGa.Core.Handler;
using Discord.Commands;

namespace ChemGa.Core.Common.Attributes;

public enum CooldownScope
{
    User,
    Guild,
    Global
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class CooldownAttribute(int seconds, CooldownScope scope = CooldownScope.User) : PreconditionAttribute
{
    public int Seconds { get; } = Math.Max(0, seconds);
    public CooldownScope Scope { get; } = scope;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (Seconds <= 0) return Task.FromResult(PreconditionResult.FromSuccess());

        if (CooldownManager.TryGetRemaining(Scope, context, command.Name, out var remaining))
        {
            return Task.FromResult(PreconditionResult.FromError($"Command is on cooldown. Try again in {Math.Ceiling(remaining.TotalSeconds)} seconds."));
        }

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
