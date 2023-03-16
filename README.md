# Sentry.Monitor

## Hangfire
### Usage

You need to add the Sentry JobFilter to the Hangfire global configuration via the `UseSentryMonitor(HttpClient, string)` helper method:

```csharp
services.AddHangfire((serviceProvider, config) => config.
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	// Add SentryMonitorJobFilter to Hangfire
	.UseSentryMonitor(
	    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("SentryMonitor"), 
	    configuration["Sentry:Dsn"]
    )
);
```

Then you need to add `[SentryMonitorId("00000000-0000-0000-0000000000")]` to your job method or the containing class, with the appropriate monitor ID retrieved from sentry.

If you use `Microsoft.Extensions.DependencyInjection` to configure your scheduler, you can use `AddHangfireSentryMonitor()` along wth `UseSentryMonitor()` extension method instead:

```csharp
// Register and configure SentryMonitorClient
services.AddSentryMonitor();

services.AddHangfire((serviceProvider, config) => config.
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	// Add SentryMonitorJobFilter to Hangfire
	.UseSentryMonitor()
);
```

## Quartz
### Usage

When configuring your scheduler, add the Sentry JobListener to the scheduler:

```csharp
scheduler.ListenerManager
    .AddSentryMonitor(
        serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("SentryMonitor"), 
        configuration["Sentry:Dsn"]
    );
```

If you use `Microsoft.Extensions.DependencyInjection` to configure your scheduler, you can use the `AddQuartzSentryMonitor()` extension method instead:

```csharp
// This call is optional and only needed if you want to add custom Quartz configuration; AddQuartzSentryMonitor also calls AddQuartz to add the listener 
services.AddQuartz(config => {});

// Add SentryMonitorJobListener to Quartz
services.AddQuartzSentryMonitor();
```

Then you need to add `[SentryMonitorId("00000000-0000-0000-0000000000")]` to your `IJob`~~~~ class, with the appropriate monitor ID retrieved from sentry.