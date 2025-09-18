using Discord;

namespace ChemGa.Interfaces;

public interface IRequirePermission
{
    GuildPermission[] Bot { get; }
    GuildPermission[] User { get; }
}

public interface ICommandMetadata
{
    CommandMetadata CommandInfo { get; }
}