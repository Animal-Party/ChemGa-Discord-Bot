using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ChemGa.Core.Handler;
using dotenv.net;
using Serilog;
using ChemGa.Core.Common.Utils;
using Serilog.Events;
using Npgsql.Replication;

internal sealed class Program
{
    private static DiscordSocketClient? Client { get; set; }
    public static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
    private static async Task MainAsync(string[] args)
    {
        try
        {
            LoadConfig();

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 100,
                GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessages
            });


            await Client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await Client.StartAsync();

            var commands = new CommandService();
            using var router = new CommandRouter(Client, commands, services: null, prefix: "!");
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