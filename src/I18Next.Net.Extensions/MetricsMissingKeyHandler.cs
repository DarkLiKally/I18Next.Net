using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

using I18Next.Net.Plugins;

namespace I18Next.Net.Extensions;

/// <summary>
///     Counts missing keys with the <c>i18next.missing_keys</c> counter of the <c>I18Next.Net</c> meter, e.g. to monitor them
///     with OpenTelemetry.
/// </summary>
public class MetricsMissingKeyHandler : IMissingKeyHandler
{
    public const string MeterName = "I18Next.Net";

    public const string CounterName = "i18next.missing_keys";

    private static readonly Meter SharedMeter = new(MeterName);

    private readonly Counter<long> _counter;

    public MetricsMissingKeyHandler()
        : this(SharedMeter)
    {
    }

    public MetricsMissingKeyHandler(Meter meter)
    {
        _counter = meter.CreateCounter<long>(CounterName, description: "Number of translations requested for missing keys.");
    }

    /// <summary>
    ///     Adds the key as tag. Only enable it with a limited number of keys as every key creates its own time series.
    /// </summary>
    public bool IncludeKey { get; set; }

    public Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args)
    {
        if (IncludeKey)
        {
            _counter.Add(1, new KeyValuePair<string, object>("i18next.language", args.Language), new KeyValuePair<string, object>("i18next.namespace", args.Namespace),
                new KeyValuePair<string, object>("i18next.key", args.Key));
        }
        else
        {
            _counter.Add(1, new KeyValuePair<string, object>("i18next.language", args.Language), new KeyValuePair<string, object>("i18next.namespace", args.Namespace));
        }

        return Task.CompletedTask;
    }
}
