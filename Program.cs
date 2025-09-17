using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ChemGa.Core.Handler;
using dotenv.net;
using Serilog;
using ChemGa.Core.Common.Utils;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection;
using ChemGa.Database;
using ChemGa;

internal sealed class Program
{
    private static DiscordSocketClient? Client { get; set; }
    private static readonly IServiceProvider _serviceProvider = ConfigureServices();
    public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
    private static async Task MainAsync(string[] args)
    {
        try
        {
            LoadConfig();

            Client = _serviceProvider.GetRequiredService<DiscordSocketClient>();

            await Client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await Client.StartAsync();

            _ = _serviceProvider.GetRequiredService<CommandService>();

            var router = _serviceProvider.GetRequiredService<CommandRouter>();
            await LoadCommands(router);

            Client.Ready += () =>
            {
                Log.Information($"Connected as {Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator} ({Client.CurrentUser.Id})");
                return Task.CompletedTask;
            };

            Client.Log += LogAsync;

            await Task.Delay(Timeout.Infinite);

        }
        finally
        {
            Log.CloseAndFlush();
            if (Client is not null)
                await Client.StopAsync();
            Client?.Dispose();
        }
    }

    public static ServiceProvider ConfigureServices()
    {
        var discordConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessages
        };

        return new ServiceCollection()
            .AddDbContext<AppDatabase>()
            .AddAttributedServices(typeof(Program).Assembly)
            .AddSingleton(discordConfig)
            .AddSingleton(sp => new DiscordSocketClient(sp.GetRequiredService<DiscordSocketConfig>()))
            .AddSingleton(sp => new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                DefaultRunMode = RunMode.Async
            }))
            .AddSingleton(sp => new CommandRouter(
                    sp.GetRequiredService<DiscordSocketClient>(),
                    sp.GetRequiredService<CommandService>(),
                    services: sp,
                    prefix: "!"
                )
            )
            .BuildServiceProvider();
    }

    private static void LoadConfig()
    {
        DotEnv.Load();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(AppConfig.AppSettings)
            .CreateLogger();
    }
    private static async Task LoadCommands(CommandRouter router)
    {
        if (Client is null) throw new InvalidOperationException("Client not initialized");
        await router.RegisterModulesAsync(typeof(Program).Assembly);
        await router.StartAsync();
        await Task.CompletedTask;
    }
    private static Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}