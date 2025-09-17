using Discord;

namespace ChemGa.Core.Interfaces;

public interface IRequirePermission
{
    GuildPermission[] Bot { get; }
    GuildPermission[] User { get; }
}

public interface ICommandMetadata
{
    CommandMetadata CommandInfo { get; }
}