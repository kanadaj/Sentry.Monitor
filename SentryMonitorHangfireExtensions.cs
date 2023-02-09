using Hangfire;

namespace Sentry.Hangfire;

public static class SentryMonitorHangfireExtensions
{
    public static IGlobalConfiguration UseSentryMonitor(this IGlobalConfiguration configuration, IHttpClientFactory httpClientFactory, string sentryDsn)
    {
        return configuration.UseFilter(new SentryMonitorJobFilter(httpClientFactory, sentryDsn));
    }
}