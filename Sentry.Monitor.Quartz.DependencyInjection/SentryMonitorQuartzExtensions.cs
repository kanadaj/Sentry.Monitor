using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Sentry.Monitor.Shared;

namespace Sentry.Monitor.Quartz.DependencyInjection;

public static class SentryMonitorQuartzExtensions
{
    public static IServiceCollection AddQuartzSentryMonitor(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<SentryMonitorJobListener>();
        serviceCollection.AddHttpClient<SentryMonitorClient>((sp, options) =>
        {
            var config = sp.GetService<IOptionsSnapshot<SentryOptions>>()?.Value;

            if (config?.Dsn == null)
            {
                return;
            }
            
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DSN", config.Dsn);
            var sentryDsnRegex = new Regex("https://([^@]+)@([^/]+)/([0-9]+)");
            var sentryHost = sentryDsnRegex.Match(config.Dsn).Groups[2].Value;
            options.BaseAddress = new Uri($"https://{sentryHost}/api/0/");
        });
        
        // AddQuartz is idempotent, so it's safe to call it multiple times.
        serviceCollection.AddQuartz(config =>
        {
            config.AddJobListener<SentryMonitorJobListener>();
        });

        return serviceCollection;
    }
}