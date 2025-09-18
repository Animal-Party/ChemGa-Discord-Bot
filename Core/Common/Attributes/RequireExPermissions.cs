using ChemGa.Core.Common.Utils;
using Discord;
using Discord.Commands;

namespace ChemGa.Core.Common.Attributes;

// Base helper for permission checks


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireUserPermissionExAttribute(GuildPermission permission) : PreconditionAttribute
{
    public GuildPermission Permission { get; } = permission;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (RequireExHelpers.IsBypassed(services, context.User.Id)) return Task.FromResult(PreconditionResult.FromSuccess());

        if (context.User is not IGuildUser gu)
            return Task.FromResult(PreconditionResult.FromError("Lệnh này chỉ có thể được sử dụng trong kênh của máy chủ."));

        if (gu.GuildPermissions.Has(Permission)) return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(PreconditionResult.FromError($"Bạn cần quyền **{Permission}** để sử dụng lệnh này."));
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireBotPermissionExAttribute : PreconditionAttribute
{
    public GuildPermission Permission { get; }
    public RequireBotPermissionExAttribute(GuildPermission permission) => Permission = permission;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (RequireExHelpers.IsBypassed(services, context.User.Id)) return Task.FromResult(PreconditionResult.FromSuccess());

        if (context.Guild == null) return Task.FromResult(PreconditionResult.FromError("Lệnh này chỉ có thể được sử dụng trong kênh của máy chủ."));

        var botUser = context.Guild.GetCurrentUserAsync().GetAwaiter().GetResult();
        if (botUser == null) return Task.FromResult(PreconditionResult.FromError("Bot không có trong máy chủ."));

        if (botUser.GuildPermissions.Has(Permission)) return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(PreconditionResult.FromError($"Bot cần quyền **{Permission}** để thực thi lệnh này."));
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireOwnerExAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (RequireExHelpers.IsBypassed(services, context.User.Id)) return Task.FromResult(PreconditionResult.FromSuccess());

        if (context.User.Id == context.Client.CurrentUser.Id) return Task.FromResult(PreconditionResult.FromSuccess());

        // Fallback to Discord.NET's RequireOwner logic if present on CommandService; here we just check bot owners from config if any
        // For now, deny if not owner (you can wire your own owner check later)
        return Task.FromResult(PreconditionResult.FromError("Chỉ chủ sở hữu bot mới có thể sử dụng lệnh này."));
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireContextExAttribute : PreconditionAttribute
{
    public ContextType[] Contexts { get; }
    public RequireContextExAttribute(params ContextType[] contexts) => Contexts = contexts;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (RequireExHelpers.IsBypassed(services, context.User.Id)) return Task.FromResult(PreconditionResult.FromSuccess());

        var isValid = Contexts.Any(c => c switch
        {
            ContextType.Guild => context.Guild != null,
            ContextType.DM => context.Guild == null,
            ContextType.Group => false,
            _ => false
        });

        if (isValid) return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(PreconditionResult.FromError("Lệnh này không được phép trong ngữ cảnh hiện tại."));
    }
}
