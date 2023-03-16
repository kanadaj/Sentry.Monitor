using System.Reflection;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Sentry.Monitor.Shared;

namespace Sentry.Monitor.Hangfire;

public class SentryMonitorJobFilter : IJobFilter, IServerFilter, IElectStateFilter
{
    public bool AllowMultiple => false;

    public int Order => 0;

    private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
    private readonly SentryMonitorClient _client;

    public SentryMonitorJobFilter(SentryMonitorClient client)
    {
        _client = client;
    }

    public void OnPerforming(PerformingContext filterContext)
    {
        var (typeName, id) = GetMethodTypeAndId(filterContext.BackgroundJob.Job.Method, filterContext.BackgroundJob.Job.Method.DeclaringType!);

        //HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue($"DSN {dsn}");

        SentrySdk.ConfigureScope(scope =>
        {
            scope.Contexts["monitor"] = new{id};
            scope.SetTag("job_id", filterContext.BackgroundJob.Id);
            scope.SetTag("job_type", typeName);
            scope.SetTag("job_method", filterContext.BackgroundJob.Job.Method.Name);
            scope.SetTag("job_arguments", string.Join(", ", filterContext.BackgroundJob.Job.Args));
        });

        if (id != null)
        {
            filterContext.SetJobParameter("start_date", DateTime.UtcNow);
            var checkinId = _client.StartCheckinAsync(id).GetAwaiter().GetResult();
            if (checkinId != null)
            {
                filterContext.SetJobParameter("checkin_id", checkinId);
            }
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        var (_, id) = GetMethodTypeAndId(filterContext.BackgroundJob.Job.Method, filterContext.BackgroundJob.Job.Method.DeclaringType!);

        if (id != null)
        {
            var checkinId = filterContext.GetJobParameter<string>("checkin_id");
            var startDate = filterContext.GetJobParameter<DateTime>("start_date");
            if (checkinId != null)
            {
                _client.FinishCheckinAsync(id, checkinId, false, (long)(DateTime.UtcNow - startDate).TotalMilliseconds).GetAwaiter().GetResult();
            }
        }
    }

    public void OnStateElection(ElectStateContext filterContext)
    {
        var (_, id) = GetMethodTypeAndId(filterContext.BackgroundJob.Job.Method, filterContext.BackgroundJob.Job.Method.DeclaringType!);

        if (filterContext.CandidateState is FailedState failedState)
        {
            if (failedState.Exception != null)
            {
                SentrySdk.CaptureException(failedState.Exception, scope =>
                {
                    scope.Contexts["monitor"] = new{id};
                    scope.SetTag("job_id", filterContext.BackgroundJob.Id);
                    if (filterContext.BackgroundJob.Job.Method.DeclaringType != null)
                    {
                        scope.SetTag("job_type", filterContext.BackgroundJob.Job.Method.DeclaringType.Name);
                    }
                    scope.SetTag("job_method", filterContext.BackgroundJob.Job.Method.Name);
                    scope.SetTag("job_arguments", string.Join(", ", filterContext.BackgroundJob.Job.Args));
                });
            }
            
            if (id != null)
            {
                var checkinId = filterContext.GetJobParameter<string>("checkin_id");
                var startDate = filterContext.GetJobParameter<DateTime>("start_date");
                if (checkinId != null)
                {
                    _client.FinishCheckinAsync(id, checkinId, true, (long)(DateTime.UtcNow - startDate).TotalMilliseconds).GetAwaiter().GetResult();
                }
            }
        }
    }

    private static (string typeName, string? id) GetMethodTypeAndId(MemberInfo methodInfo, MemberInfo declaringType)
    {
        var methodAttribute = methodInfo.GetCustomAttributes(typeof(SentryMonitorIdAttribute), true)
            .Cast<SentryMonitorIdAttribute>()
            .Select(x => x.Id)
            .FirstOrDefault();

        if (methodAttribute != null)
        {
            return (declaringType.Name + "." + methodInfo.Name, methodAttribute);
        }

        var id = declaringType?.GetCustomAttributes(typeof(SentryMonitorIdAttribute), true)
            .Cast<SentryMonitorIdAttribute>()
            .Select(x => x.Id)
            .FirstOrDefault();
        return (declaringType?.Name + "." + methodInfo.Name, id);
    }
}