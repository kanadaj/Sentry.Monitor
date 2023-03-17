using System.Reflection;
using Quartz;
using Sentry.Monitor.Shared;

namespace Sentry.Monitor.Quartz;

public class SentryMonitorJobListener : IJobListener
{
    private readonly SentryMonitorClient _client;

    public SentryMonitorJobListener(SentryMonitorClient client)
    {
        _client = client;
    }

    public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var (typeName, id) = GetMethodTypeAndId(context.JobDetail.JobType);

            //HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue($"DSN {dsn}");

            if (id != null)
            {
                context.Put("start_date", DateTime.UtcNow);
                var checkinId = await _client.StartCheckinAsync(id, cancellationToken);
                if (checkinId != null)
                {
                    context.Put("checkin_id", checkinId);
                }
            }
        }
        catch
        {
            // TODO: Add logging
        }
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var (typeName, id) = GetMethodTypeAndId(context.JobDetail.JobType);

            if (id != null)
            {
                if (jobException != null)
                {
                    SentrySdk.CaptureException(jobException, scope =>
                    {
                        scope.Contexts["monitor"] = new{id};
                        scope.SetTag("job_id", context.FireInstanceId);
                        scope.SetTag("job_type", typeName);
                        scope.SetTag("job_arguments", string.Join(", ", context.JobDetail.JobDataMap.Values));
                    });
                }
            
                var checkinId = context.Get("checkin_id") as string;
                var startDate = context.Get("start_date") as DateTime?;
                if (checkinId != null)
                {
                    await _client.FinishCheckinAsync(id, checkinId, jobException != null, (long)((DateTime.UtcNow - startDate)?.TotalMilliseconds ?? 0), cancellationToken);
                }
            }
        }
        catch
        {
            // TODO: Add logging
        }
    }

    public string Name => nameof(SentryMonitorJobListener);

    private static (string typeName, string? id) GetMethodTypeAndId(MemberInfo declaringType)
    {
        var id = declaringType?.GetCustomAttributes(typeof(SentryMonitorIdAttribute), true)
            .Cast<SentryMonitorIdAttribute>()
            .Select(x => x.Id)
            .FirstOrDefault();
        return (declaringType?.Name ?? string.Empty, id);
    }
}