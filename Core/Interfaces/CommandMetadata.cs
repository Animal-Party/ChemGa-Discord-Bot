using System.Reflection;
using Discord;
using Discord.Commands;

namespace ChemGa.Core.Interfaces;

public record PermissionRequirement(
    GuildPermission[] UserPermissions,
    GuildPermission[] BotPermissions
);

public record CommandMetadata(
    string Name,
    string Description,
    string[] Aliases,
    string Category,
    Type DeclaringType,
    MethodInfo? Method,
    int CooldownSeconds,
    // Permissions requirements (user, bot)
    GuildPermission[]? UserPermissions,
    GuildPermission[]? BotPermissions,
    // Remarks / additional help text
    string? Remarks,
    // Priority (higher wins)
    int Priority,
    // Whether command requires guild / dm / group contexts
    ContextType[]? RequireContexts,
    // Whether command requires owner
    bool RequireOwner,
    // Any other preconditions represented as their type names
    string[] Preconditions,
    // Required role names
    string[] RequiredRoles
    ,
    // Group(s) the module/class belongs to
    string[]? Groups
);
