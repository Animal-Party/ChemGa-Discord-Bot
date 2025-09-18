using Microsoft.Extensions.Configuration;

namespace ChemGa.Core.Common.Utils;

public static class AppConfig
{
    private static IConfigurationRoot? _configuration;

    public static IConfigurationRoot AppSettings
    {
        get
        {
            if (_configuration == null)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Console.WriteLine($"Loading appsettings from: {GetAppSettingsPath()}");
                _configuration = builder.Build();
            }
            return _configuration;
        }
    }

    private static string GetAppSettingsPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    public static string GetPrefix()
    {
        return AppSettings["Properties:Prefix"] ?? "c";
    }
}
