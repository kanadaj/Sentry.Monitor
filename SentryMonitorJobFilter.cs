using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Sentry.Hangfire;

public class SentryMonitorJobFilter : IJobFilter, IServerFilter, IElectStateFilter
{
    public bool AllowMultiple => false;

    private readonly Dictionary<string, (string checkinId, DateTime startDate)> _checkins = new();

    public int Order => 0;

    private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

    private readonly HttpClient _httpClient;
    private readonly string _sentryHost;

    public SentryMonitorJobFilter(IHttpClientFactory httpClientFactory, string sentryDsn)
    {
        _httpClient = httpClientFactory.CreateClient("SentryMonitorJobFilter");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DSN", sentryDsn);
        var sentryDsnRegex = new Regex("https://([^@]+)@([^/]+)/([0-9]+)");
        _sentryHost = sentryDsnRegex.Match(sentryDsn).Groups[2].Value;
    }

    private string Url(string id) => $"https://{_sentryHost}/api/0/monitors/{id}/checkins/";

    public void OnPerforming(PerformingContext filterContext)
    {
        var (typeName, id) = GetMethodTypeAndId(filterContext.BackgroundJob.Job.Method, filterContext.BackgroundJob.Job.Method.DeclaringType);

        //HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue($"DSN {dsn}");

        SentrySdk.ConfigureScope(scope =>
        {
            scope.Contexts["monitor"] = id;
            scope.SetTag("job_id", filterContext.BackgroundJob.Id);
            scope.SetTag("job_type", typeName);
            scope.SetTag("job_method", filterContext.BackgroundJob.Job.Method.Name);
            scope.SetTag("job_arguments", string.Join(", ", filterContext.BackgroundJob.Job.Args));
        });

        if (id != null)
        {
            var checkinId = filterContext.BackgroundJob.Id;
            filterContext.SetJobParameter("start_date", DateTime.UtcNow);
            _httpClient.PostAsync(Url(id), new StringContent(JsonConvert.SerializeObject(new
            {
                status = "in_progress",
                checkin_id = checkinId,
            }), Encoding.UTF8, "application/json"));
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        var (_, id) = GetMethodTypeAndId(filterContext.BackgroundJob.Job.Method, filterContext.BackgroundJob.Job.Method.DeclaringType);

        if (id != null && _checkins.ContainsKey(filterContext.BackgroundJob.Id))
        {
            var checkinId = filterContext.BackgroundJob.Id;
            var startDate = filterContext.GetJobParameter<DateTime>("start_date");
            if (checkinId != null)
            {
                _httpClient.PostAsync(Url(id), new StringContent(JsonConvert.SerializeObject(new
                {
                    status = "ok",
                    duration = (long)(DateTime.UtcNow - startDate).TotalMilliseconds,
                    checkin_id = checkinId,
                }), Encoding.UTF8, "application/json"));
            }
        }
    }

    public void OnStateElection(ElectStateContext filterContext)
    {
        var (_, id) = GetMethodTypeAndId(filterContext.BackgroundJob.Job.Method, filterContext.BackgroundJob.Job.Method.DeclaringType);

        if (filterContext.CandidateState is FailedState failedState)
        {
            if (id != null && _checkins.ContainsKey(filterContext.BackgroundJob.Id))
            {
                var checkinId = filterContext.BackgroundJob.Id;
                var startDate = filterContext.GetJobParameter<DateTime>("start_date");
                if (checkinId != null)
                {
                    _httpClient.PostAsync(Url(id), new StringContent(JsonConvert.SerializeObject(new
                    {
                        status = "error",
                        duration = (long)(DateTime.UtcNow - startDate).TotalMilliseconds,
                        checkin_id = checkinId,
                    }), Encoding.UTF8, "application/json"));
                }
            }
        }
    }

    private static (string typeName, string id) GetMethodTypeAndId(MemberInfo methodInfo, MemberInfo declaringType)
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