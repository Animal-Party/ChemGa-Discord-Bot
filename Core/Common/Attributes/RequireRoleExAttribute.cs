using Discord;
using Discord.Commands;

namespace ChemGa.Core.Common.Attributes;

/// <summary>
/// Enhanced RequireRoleAttribute that supports formatting placeholders in ErrorMessage.
/// Supported placeholders: {roleId}, {roleName}
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequireRoleExAttribute : PreconditionAttribute
{
    public string? RoleName { get; }
    public ulong? RoleId { get; }

    /// <summary>
    /// Optional error message when not in a guild context.
    /// </summary>
    public string? NotAGuildErrorMessage { get; set; }

    public RequireRoleExAttribute(ulong roleId)
    {
        RoleId = roleId;
    }

    public RequireRoleExAttribute(string roleName)
    {
        RoleName = roleName;
    }

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        try
        {
            if (services?.GetService(typeof(Services.IBypassService)) is Services.IBypassService bypass)
            {
                var bypassed = bypass.IsBypassedAsync(context.User.Id).GetAwaiter().GetResult();
                if (bypassed) return Task.FromResult(PreconditionResult.FromSuccess());
            }
        }
        catch
        {
        }

        if (context.User is not IGuildUser guildUser)
            return Task.FromResult(PreconditionResult.FromError(NotAGuildErrorMessage ?? "Lệnh này chỉ có thể được sử dụng trong kênh của máy chủ."));

        if (RoleId.HasValue)
        {
            if (guildUser.RoleIds.Contains(RoleId.Value))
                return Task.FromResult(PreconditionResult.FromSuccess());

            var role = context.Guild?.GetRole(RoleId.Value);
            var roleName = role?.Name ?? RoleId.Value.ToString();
            var message = FormatErrorMessage(ErrorMessage, RoleId.Value, roleName);
            return Task.FromResult(PreconditionResult.FromError(message ?? $"Người dùng cần có role {roleName}."));
        }

        if (!string.IsNullOrEmpty(RoleName))
        {
            var roleNames = guildUser.RoleIds.Select(x => guildUser.Guild.GetRole(x)?.Name).Where(x => x != null).Cast<string>();

            if (roleNames.Contains(RoleName))
                return Task.FromResult(PreconditionResult.FromSuccess());

            var message = FormatErrorMessage(ErrorMessage, null, RoleName);
            return Task.FromResult(PreconditionResult.FromError(message ?? $"Người dùng cần có role {RoleName}."));
        }

        return Task.FromResult(PreconditionResult.FromSuccess());
    }

    private static string? FormatErrorMessage(string? template, ulong? roleId, string? roleName)
    {
        if (string.IsNullOrEmpty(template))
            return null;

        return template.Replace("{roleId}", roleId?.ToString() ?? string.Empty)
                       .Replace("{roleName}", roleName ?? string.Empty);
    }
}
