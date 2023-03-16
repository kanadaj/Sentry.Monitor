using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sentry.Monitor.Shared;

public class SentryMonitorClient
{
    private readonly HttpClient _httpClient;
    
    public SentryMonitorClient(HttpClient httpClient, string sentryDsn)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DSN", sentryDsn);
        var sentryDsnRegex = new Regex("https://([^@]+)@([^/]+)/([0-9]+)");
        var sentryHost = sentryDsnRegex.Match(sentryDsn).Groups[2].Value;
        _httpClient.BaseAddress = new Uri($"https://{sentryHost}/api/0/");
    }
    
    public SentryMonitorClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private string CreateCheckinUrl(string id) => $"monitors/{id}/checkins/";
    private string UpdateCheckinUrl(string id, string checkinId) => $"monitors/{id}/checkins/{checkinId}/";
    
    public async Task<string?> StartCheckinAsync(string jobId, CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await _httpClient.PostAsync(CreateCheckinUrl(jobId), new StringContent(JsonSerializer.Serialize(new
        {
            status = "in_progress",
            checkin_id = jobId,
        }), Encoding.UTF8, "application/json"), cancellationToken);

        if (result.IsSuccessStatusCode)
        {
            var json = await result.Content.ReadAsStringAsync();
            var checkin = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return checkin?["id"] as string;
        }

        return null;
    }
    
    public async Task FinishCheckinAsync(string jobId, string checkinId, bool isError, long duration, CancellationToken cancellationToken = new CancellationToken())
    {
        await _httpClient.PutAsync(UpdateCheckinUrl(jobId, checkinId), new StringContent(JsonSerializer.Serialize(new
        {
            status = !isError ? "ok" : "error",
            duration = duration,
            checkin_id = checkinId,
        }), Encoding.UTF8, "application/json"), cancellationToken);
    }
}