using ChemGa.Database;
using ChemGa.Database.Models.Utility;
using Microsoft.EntityFrameworkCore;
using ChemGa.Core.Common.Attributes;

namespace ChemGa.Core.Services;

public interface IBypassService
{
    Task<bool> IsBypassedAsync(ulong userId);
    Task AddBypassAsync(ulong userId);
    Task RemoveBypassAsync(ulong userId);
}

[RegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, serviceType: typeof(IBypassService))]
public class GlobalBypassService : IBypassService
{
    private readonly AppDatabase _db;
    private readonly DbSet<GlobalBypass> _bypasses;

    public GlobalBypassService(AppDatabase db)
    {
        _db = db;
        _bypasses = db.GetModel<GlobalBypass>();
    }

    public async Task<bool> IsBypassedAsync(ulong userId)
    {
        return await _bypasses.AnyAsync(b => b.UserId == userId).ConfigureAwait(false);
    }

    public async Task AddBypassAsync(ulong userId)
    {
        if (!await _bypasses.AnyAsync(b => b.UserId == userId).ConfigureAwait(false))
        {
            await _bypasses.AddAsync(new GlobalBypass { UserId = userId }).ConfigureAwait(false);
            await _db.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public async Task RemoveBypassAsync(ulong userId)
    {
        var item = await _bypasses.FirstOrDefaultAsync(b => b.UserId == userId).ConfigureAwait(false);
        if (item != null)
        {
            _bypasses.Remove(item);
            await _db.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
