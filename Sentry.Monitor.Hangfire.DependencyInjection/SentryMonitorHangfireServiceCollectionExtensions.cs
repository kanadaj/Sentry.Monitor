using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sentry.Monitor.Shared;

namespace Sentry.Monitor.Hangfire.DependencyInjection;

public static class SentryMonitorHangfireServiceCollectionExtensions
{
    public static IServiceCollection AddHangfireSentryMonitor(this IServiceCollection services)
    {
        services.AddHttpClient<SentryMonitorClient>((sp, options) =>
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
        services.AddSingleton<SentryMonitorJobFilter>();
        return services;
    }
    
    public static IGlobalConfiguration UseSentryMonitor(this IGlobalConfiguration configuration, IServiceProvider services)
    {
        var jobFilter = services.GetRequiredService<SentryMonitorJobFilter>();
        return configuration.UseFilter(jobFilter);
    }
}