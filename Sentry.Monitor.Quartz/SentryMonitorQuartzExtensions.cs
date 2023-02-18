using Quartz;

namespace Sentry.Monitor.Quartz;

public static class SentryMonitorQuartzExtensions
{
    public static IListenerManager AddSentryQuartzMonitor(this IListenerManager listenerManager, HttpClient httpClient, string sentryDsn)
    {
        listenerManager.AddJobListener(new SentryMonitorScheduleListener(httpClient, sentryDsn));
        return listenerManager;
    }
}