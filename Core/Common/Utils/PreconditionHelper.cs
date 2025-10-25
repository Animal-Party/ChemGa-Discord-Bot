using ChemGa.Core.Common.Attributes;

namespace ChemGa.Core.Common.Utils;

[RegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton, typeof(RequireExHelpers))]
public static class RequireExHelpers
{
    public static bool IsBypassed(IServiceProvider? services, ulong userId)
    {
        try
        {
            if (services?.GetService(typeof(Services.IBypassService)) is Services.IBypassService bypass)
            {
                var bypassed = bypass.IsBypassedAsync(userId).GetAwaiter().GetResult();
                return bypassed;
            }
        }
        catch { }
        return false;
    }
}