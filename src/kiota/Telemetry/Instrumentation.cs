using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace kiota.Telemetry;

public class Instrumentation : IDisposable
{
    private readonly Meter _meter = new(TelemetryLabels.ScopeName);

    public ActivitySource ActivitySource { get; } = new(TelemetryLabels.ScopeName);

    /// <summary>
    /// Creates a histogram instrument used to time command duration
    /// </summary>
    /// <param name="tags">The tags to attach to the instrument</param>
    /// <returns></returns>
    public Histogram<double> CreateCommandDurationHistogram(IEnumerable<KeyValuePair<string, object?>>? tags)
    {
        return _meter.CreateHistogram<double>(name: TelemetryLabels.InstrumentCommandDurationName, unit: "s",
            description: "Duration of the command", tags: tags);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.ActivitySource.Dispose();
        this._meter.Dispose();
    }
}
