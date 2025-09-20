using System.Reflection;
using ChemGa.Core.Common.Attributes;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace ChemGa;

public static class Global
{
    public static string ToTitleCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        if (str.Length == 1) return str.ToUpperInvariant();
        return char.ToUpperInvariant(str[0]) + str[1..].ToLowerInvariant();
    }
}
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAttributedServices(this IServiceCollection services, Assembly assembly)
    {
        var typesWithAttr = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<RegisterServiceAttribute>() != null && t.IsClass && !t.IsAbstract);

        foreach (var type in typesWithAttr)
        {
            var attr = type.GetCustomAttribute<RegisterServiceAttribute>()!;
            var serviceType = attr.ServiceType ?? type;

            services.Add(new ServiceDescriptor(serviceType, type, attr.Lifetime));

            var interfaces = type.GetInterfaces()
                .Where(i => i != typeof(IDisposable));

            foreach (var iface in interfaces)
            {
                if (!services.Any(sd => sd.ServiceType == iface && sd.ImplementationType == type))
                {
                    services.Add(new ServiceDescriptor(iface, type, attr.Lifetime));
                }
            }
        }

        return services;
    }
}

public static class DiscordSocketClientExtensions
{
    public static EmbedBuilder Embed(this DiscordSocketClient _client)
    {
        return new EmbedBuilder()
            .WithColor(Color.TryParse("#aed7f3", out Color color) ? color : Color.Magenta);
    }
}
public static partial class GlobalDev
{
    public static readonly ulong[] DevIds = [1216638785463390299UL];
}

public static class InteractionServiceExtensions
{
    public static async Task AddModulesWithNestedAsync(
        this InteractionService service,
        Assembly assembly,
        IServiceProvider services)
    {
        await service.AddModulesAsync(assembly, services);

        var nestedModules = assembly.GetTypes()
            .SelectMany(t => t.GetNestedTypes(BindingFlags.Public))
            .Where(t => typeof(InteractionModuleBase<SocketInteractionContext>).IsAssignableFrom(t));

        foreach (var module in nestedModules)
        {
            await service.AddModuleAsync(module, services);
        }
    }
}