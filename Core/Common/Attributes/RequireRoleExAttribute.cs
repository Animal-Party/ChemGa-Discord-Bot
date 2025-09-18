using System;
using System.Linq;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;

namespace ChemGa.Core.Common.Attributes
{
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
            if (context.User is not IGuildUser guildUser)
                return Task.FromResult(PreconditionResult.FromError(NotAGuildErrorMessage ?? "Command must be used in a guild channel."));

            if (RoleId.HasValue)
            {
                if (guildUser.RoleIds.Contains(RoleId.Value))
                    return Task.FromResult(PreconditionResult.FromSuccess());

                var role = context.Guild.GetRole(RoleId.Value);
                var roleName = role?.Name ?? RoleId.Value.ToString();
                var message = FormatErrorMessage(ErrorMessage, RoleId.Value, roleName);
                return Task.FromResult(PreconditionResult.FromError(message ?? $"User requires guild role {roleName}."));
            }

            if (!string.IsNullOrEmpty(RoleName))
            {
                var roleNames = guildUser.RoleIds.Select(x => guildUser.Guild.GetRole(x)?.Name).Where(x => x != null).Cast<string>();

                if (roleNames.Contains(RoleName))
                    return Task.FromResult(PreconditionResult.FromSuccess());

                var message = FormatErrorMessage(ErrorMessage, null, RoleName);
                return Task.FromResult(PreconditionResult.FromError(message ?? $"User requires guild role {RoleName}."));
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
}
