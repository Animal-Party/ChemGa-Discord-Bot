using Microsoft.Extensions.DependencyInjection;
using ChemGa.Database;
using ChemGa.Core.Common.Attributes;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace ChemGa.Core.Common.Utils;

public interface IDbWriteLocker
{
    Task RunAsync(Func<AppDatabase, Task> action);
    Task<T> RunAsync<T>(Func<AppDatabase, Task<T>> action);
}

[RegisterService(ServiceLifetime.Singleton, serviceType: typeof(IDbWriteLocker))]
public class DbWriteLocker(IServiceScopeFactory scopeFactory) : IDbWriteLocker, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task RunAsync(Func<AppDatabase, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDatabase>();
            try
            {
                await action(db).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log full exception details including inner exceptions and EF entries when available
                Log.Error(ex, "DB write action failed: {Message}", ex.Message);
                if (ex is DbUpdateException dbEx)
                {
                    try
                    {
                        Log.Error("DbUpdateException inner: {Inner}", dbEx.InnerException?.ToString());
                        foreach (var entry in dbEx.Entries)
                        {
                            Log.Error("Failed entity: {EntityType} state={State}", entry.Entity?.GetType().FullName, entry.State);
                        }
                    }
                    catch (Exception innerLogEx)
                    {
                        Log.Warning(innerLogEx, "Failed while logging DbUpdateException details");
                    }
                }

                // Re-throw so callers still receive the exception
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T> RunAsync<T>(Func<AppDatabase, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDatabase>();
            try
            {
                return await action(db).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DB write action failed (generic): {Message}", ex.Message);
                if (ex is DbUpdateException dbEx)
                {
                    try
                    {
                        Log.Error("DbUpdateException inner: {Inner}", dbEx.InnerException?.ToString());
                        foreach (var entry in dbEx.Entries)
                        {
                            Log.Error("Failed entity: {EntityType} state={State}", entry.Entity?.GetType().FullName, entry.State);
                        }
                    }
                    catch (Exception innerLogEx)
                    {
                        Log.Warning(innerLogEx, "Failed while logging DbUpdateException details");
                    }
                }

                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
