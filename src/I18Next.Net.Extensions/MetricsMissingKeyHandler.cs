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
    /// <summary>
    ///     The name of the meter used by the parameterless constructor, <c>I18Next.Net</c>.
    /// </summary>
    public const string MeterName = "I18Next.Net";

    /// <summary>
    ///     The name of the counter, <c>i18next.missing_keys</c>.
    /// </summary>
    public const string CounterName = "i18next.missing_keys";

    private static readonly Meter SharedMeter = new(MeterName);

    private readonly Counter<long> _counter;

    /// <summary>
    ///     Creates a handler counting with a meter named <see cref="MeterName" /> shared by all handlers.
    /// </summary>
    public MetricsMissingKeyHandler()
        : this(SharedMeter)
    {
    }

    /// <summary>
    ///     Creates a handler counting with the given meter, e.g. one created by an <c>IMeterFactory</c>.
    /// </summary>
    /// <param name="meter">The meter creating the <see cref="CounterName" /> counter.</param>
    public MetricsMissingKeyHandler(Meter meter)
    {
        _counter = meter.CreateCounter<long>(CounterName, description: "Number of translations requested for missing keys.");
    }

    /// <summary>
    ///     Adds the key as tag. Only enable it with a limited number of keys as every key creates its own time series.
    /// </summary>
    public bool IncludeKey { get; set; }

    /// <summary>
    ///     Increments the counter, tagged with <c>i18next.language</c> and <c>i18next.namespace</c>, and
    ///     <c>i18next.key</c> if <see cref="IncludeKey" /> is enabled.
    /// </summary>
    /// <param name="sender">The calling translator.</param>
    /// <param name="args">The language, namespace and key of the missing translation.</param>
    /// <returns>A completed task.</returns>
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
