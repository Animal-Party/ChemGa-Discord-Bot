namespace ChemGa.Core.Common.Utils;

public static class RequireExHelpers
{
    public static bool IsBypassed(IServiceProvider? services, ulong userId)
    {
        try
        {
            var bypass = services?.GetService(typeof(Services.IBypassService)) as Services.IBypassService;
            if (bypass != null)
            {
                var bypassed = bypass.IsBypassedAsync(userId).GetAwaiter().GetResult();
                return bypassed;
            }
        }
        catch { }
        return false;
    }
}