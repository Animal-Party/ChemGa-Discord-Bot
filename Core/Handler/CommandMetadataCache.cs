using System.Collections.Concurrent;
using ChemGa.Core.Interfaces;
namespace ChemGa.Core.Handler;

/// <summary>
/// Builds and caches CommandMetadata objects keyed by Discord.Commands.CommandInfo.
/// This avoids rescanning assemblies repeatedly from modules like HelpModule.
/// </summary>
public static class CommandMetadataCache
{
    private static readonly ConcurrentDictionary<string, CommandMetadata> _map = new();
    public static bool TryAdd(string commandName, CommandMetadata meta) => _map.TryAdd(commandName, meta);
    public static bool TryGet(string commandName, out CommandMetadata? meta) => _map.TryGetValue(commandName, out meta);

    public static IEnumerable<CommandMetadata> GetAll() => _map.Values;
}
