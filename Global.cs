using System.Reflection;
using ChemGa.Core.Common.Attributes;
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
        }

        return services;
    }
}
