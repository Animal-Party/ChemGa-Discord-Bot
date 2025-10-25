using System.Reflection;
using ChemGa.Core.Common.Attributes;
using ChemGa.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ChemGa.Core.Background;

[RegisterService(ServiceLifetime.Singleton, serviceType: typeof(CronSchedulerService))]
public sealed class CronSchedulerService(IServiceProvider serviceProvider) : IService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly List<Task> _runners = [];
    private CancellationTokenSource? _cts;

    private record JobBase(string Expression, CronSchedule Schedule);
    private sealed record ClassJob(Type Type, CronSchedule Schedule, string Expression) : JobBase(Expression, Schedule);
    private sealed record MethodJob(Type DeclaringType, MethodInfo Method, CronSchedule Schedule, string Expression) : JobBase(Expression, Schedule);

    private List<ClassJob> _classJobs = [];
    private List<MethodJob> _methodJobs = [];

    public Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        var asm = typeof(Program).Assembly;

        _classJobs = [.. asm.GetTypes()
            .Where(t => typeof(ICronJob).IsAssignableFrom(t) && t.GetCustomAttribute<CronJobAttribute>() != null)
            .Select(t => new ClassJob(
                t,
                CronSchedule.Parse(t.GetCustomAttribute<CronJobAttribute>()!.Expression),
                t.GetCustomAttribute<CronJobAttribute>()!.Expression))
            ];

        _methodJobs = [.. asm.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            .Where(m => m.GetCustomAttribute<CronJobAttribute>() != null)
            .Select(m => new MethodJob(
                m.DeclaringType!,
                m,
                CronSchedule.Parse(m.GetCustomAttribute<CronJobAttribute>()!.Expression),
                m.GetCustomAttribute<CronJobAttribute>()!.Expression))
            ];

        foreach (var job in _classJobs)
            _runners.Add(RunClassJobLoopAsync(job, _cts.Token));

        foreach (var job in _methodJobs)
            _runners.Add(RunMethodJobLoopAsync(job, _cts.Token));

        Log.Information("CronScheduler started with {count} jobs", _runners.Count);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts == null) return;
        _cts.Cancel();

        try { await Task.WhenAny(Task.WhenAll(_runners), Task.Delay(5000)); }
        catch (OperationCanceledException) { }

        _cts.Dispose();
        _cts = null;
        _runners.Clear();
    }

    private async Task RunClassJobLoopAsync(ClassJob job, CancellationToken token)
    {
        // Tick every second; compute next run via cron and trigger when reached.
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var next = job.Schedule.GetNext(DateTimeOffset.UtcNow);
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                var now = DateTimeOffset.UtcNow;
                if (now < next) continue;

                using var scope = _serviceProvider.CreateScope();
                try
                {
                    var instance = scope.ServiceProvider.GetService(job.Type)
                        ?? ActivatorUtilities.CreateInstance(scope.ServiceProvider, job.Type);

                    if (instance is ICronJob cronJob)
                        await cronJob.ExecuteAsync(token);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Cron class job failed: {job}", job.Type.Name);
                }

                do { next = job.Schedule.GetNext(next); } while (next <= DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RunMethodJobLoopAsync(MethodJob job, CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var next = job.Schedule.GetNext(DateTimeOffset.UtcNow);
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                var now = DateTimeOffset.UtcNow;
                if (now < next) continue;

                using var scope = _serviceProvider.CreateScope();
                try
                {
                    var instance = scope.ServiceProvider.GetService(job.DeclaringType)
                        ?? ActivatorUtilities.CreateInstance(scope.ServiceProvider, job.DeclaringType);

                    if (instance == null)
                    {
                        do { next = job.Schedule.GetNext(next); } while (next <= DateTimeOffset.UtcNow);
                        continue;
                    }

                    var parameters = job.Method.GetParameters();
                    object? result = parameters.Length switch
                    {
                        0 => job.Method.Invoke(instance, null),
                        1 when parameters[0].ParameterType == typeof(CancellationToken) => job.Method.Invoke(instance, [token]),
                        _ => null
                    };

                    if (result is Task task) await task.ConfigureAwait(false);
                    else if (result is ValueTask vt) await vt.ConfigureAwait(false);
                }
                catch (TargetInvocationException tex) when (tex.InnerException is OperationCanceledException) { }
                catch (TargetInvocationException tex)
                {
                    Log.Warning(tex.InnerException ?? tex, "Cron method job failed: {type}.{method}",
                                job.DeclaringType.Name, job.Method.Name);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Cron method job failed: {type}.{method}", job.DeclaringType.Name, job.Method.Name);
                }

                do { next = job.Schedule.GetNext(next); } while (next <= DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException) { }
    }
}
