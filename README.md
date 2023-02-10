# Sentry.Hangfire

## Usage

You need to add the Sentry JobFilter to the Hangfire global configuration via the `UseSentryMonitor` helper method:

```csharp
services.AddHangfire((serviceProvider, config) => config.
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	.UseSentryMonitor(serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("HangfireSentryMonitor"), configuration["Sentry:Dsn"])
);
```

Then you need to add `[SentryMonitorId("00000000-0000-0000-0000000000")]` to your job method or the containing class, with the appropriate monitor ID retrieved from sentry.