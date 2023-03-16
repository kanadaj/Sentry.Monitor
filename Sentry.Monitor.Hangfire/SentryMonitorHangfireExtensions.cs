using Hangfire;
using Sentry.Monitor.Shared;

namespace Sentry.Monitor.Hangfire;

public static class SentryMonitorHangfireExtensions
{
    public static IGlobalConfiguration UseSentryMonitor(this IGlobalConfiguration configuration, HttpClient httpClient, string sentryDsn)
    {
        return configuration.UseFilter(new SentryMonitorJobFilter(new SentryMonitorClient(httpClient, sentryDsn)));
    }
}