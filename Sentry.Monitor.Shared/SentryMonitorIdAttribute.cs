namespace Sentry.Monitor.Shared;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class SentryMonitorIdAttribute : Attribute
{
    public string Id { get; }

    public SentryMonitorIdAttribute(string id)
    {
        Id = id;
    }
}