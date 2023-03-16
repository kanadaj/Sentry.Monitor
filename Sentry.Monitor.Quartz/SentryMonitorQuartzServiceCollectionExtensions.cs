using Quartz;
using Sentry.Monitor.Shared;

namespace Sentry.Monitor.Quartz;

public static class SentryMonitorQuartzServiceCollectionExtensions
{
    public static IListenerManager AddSentryMonitor(this IListenerManager listenerManager, HttpClient httpClient, string sentryDsn)
    {
        var client = new SentryMonitorClient(httpClient, sentryDsn);
        listenerManager.AddJobListener(new SentryMonitorJobListener(client));
        return listenerManager;
    }
}