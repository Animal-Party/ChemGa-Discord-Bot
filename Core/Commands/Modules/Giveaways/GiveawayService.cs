using ChemGa.Core.Common.Attributes;
using ChemGa.Database;
using ChemGa.Database.Models;
using ChemGa.Interfaces;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace ChemGa.Core.Commands.Modules.Giveaways;

[RegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, serviceType: typeof(GiveawayService))]
public class GiveawayService(AppDatabase db, DiscordSocketClient _client) : IService
{
    private readonly AppDatabase _db = db;
    private readonly DiscordSocketClient _client = _client;
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private readonly DbSet<Giveaway> _giveaways = db.GetModel<Giveaway>();
    /// <summary>
    /// Lấy danh sách giveaway còn hoạt động (chưa hết hạn).
    /// </summary>
    public IQueryable<Giveaway> GetActiveGiveaways()
    {
        return _giveaways.Where(g => g.EndAt > DateTime.UtcNow && !g.IsEnded);
    }

    /// <summary>
    /// Lấy danh sách giveaway đã hết hạn nhưng chưa xử lý.
    /// </summary>
    public IQueryable<Giveaway> GetEndedGiveaways()
    {
        return _giveaways.Where(g => g.EndAt <= DateTime.UtcNow && !g.IsEnded);
    }

    /// <summary>
    /// Bắt đầu một giveaway mới và lưu vào database.
    /// </summary>
    public async Task<Giveaway> StartGiveawayAsync(
        string prize,
        ulong guildId,
        ulong channelId,
        ulong messageId,
        ulong hostId,
        int winnerCount,
        TimeSpan duration
    )
    {
        var giveaway = new Giveaway
        {
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.Add(duration),
            Prize = prize,
            GuildId = guildId,
            ChannelId = channelId,
            MessageId = messageId,
            HostId = hostId,
            WinnerCount = winnerCount,
            IsEnded = false
        };

        await _giveaways.AddAsync(giveaway);
        await _db.SaveChangesAsync();

        return giveaway;
    }

    /// <summary>
    /// Đánh dấu giveaway đã kết thúc.
    /// </summary>
    public async Task EndGiveawayAsync(Giveaway giveaway)
    {
        giveaway.IsEnded = true;
        _giveaways.Update(giveaway);
        await _db.SaveChangesAsync();
    }

    private async Task ReCheckGiveaway(CancellationToken token)
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(token))
        {
            var endedGiveaways = GetEndedGiveaways().ToList();
            foreach (var giveaway in endedGiveaways)
            {
                await EndGiveawayAsync(giveaway);
            }
        }
    }

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        _worker = ReCheckGiveaway(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_worker != null)
            {
                await _worker;
            }
            _cts.Dispose();
            _cts = null;
        }

        await _db.SaveChangesAsync();
    }

}