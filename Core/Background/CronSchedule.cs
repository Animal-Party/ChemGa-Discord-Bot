using NCrontab;

namespace ChemGa.Core.Background;

internal sealed class CronSchedule
{
    private readonly CrontabSchedule _schedule;

    private CronSchedule(CrontabSchedule schedule)
    {
        _schedule = schedule;
    }

    public static CronSchedule Parse(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) throw new ArgumentException("Cron expression is empty", nameof(expr));
        var opts = new CrontabSchedule.ParseOptions { IncludingSeconds = true };
        var schedule = CrontabSchedule.Parse(expr, opts);
        return new CronSchedule(schedule);
    }

    public DateTimeOffset GetNext(DateTimeOffset from)
    {
        var next = _schedule.GetNextOccurrence(from.UtcDateTime);
        return new DateTimeOffset(next, TimeSpan.Zero);
    }
}
