using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentry.Hangfire;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class SentryMonitorIdAttribute : Attribute
{
    public string Id { get; }

    public SentryMonitorIdAttribute(string id)
    {
        Id = id;
    }
}