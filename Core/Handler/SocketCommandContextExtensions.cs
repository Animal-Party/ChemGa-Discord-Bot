using ChemGa.Interfaces;
using Discord.Commands;

namespace ChemGa.Core.Handler;

public static class SocketCommandContextExtensions
{
    public static bool TryGetCommandMetadata(this SocketCommandContext context, CommandInfo cmd, out CommandMetadata? meta)
    {
        return CommandMetadataCache.TryGet(cmd.Name, out meta);
    }

    public static IEnumerable<CommandMetadata> GetAllCommandMetadata(this SocketCommandContext context)
    {
        return CommandMetadataCache.GetAll();
    }
}
