namespace CmcHomework.Api.Services;

public sealed class AppLifetime
{
    public AppLifetime()
    {
        StartedAt = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset StartedAt { get; }

    public long GetUptimeSeconds(DateTimeOffset now)
    {
        var seconds = (long)(now - StartedAt).TotalSeconds;
        return Math.Max(0, seconds);
    }
}
