using Hangfire;

namespace Sentry.Monitor.Hangfire;

public static class SentryMonitorHangfireExtensions
{
    public static IGlobalConfiguration UseSentryHangfireMonitor(this IGlobalConfiguration configuration, HttpClient httpClient, string sentryDsn)
    {
        return configuration.UseFilter(new SentryMonitorJobFilter(httpClient, sentryDsn));
    }
}