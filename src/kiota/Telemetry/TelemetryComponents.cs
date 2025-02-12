using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace kiota.Telemetry;

public class TelemetryComponents : IDisposable
{
    private static ActivitySource? _activitySource;
    private static Meter? _meter;

    public ActivitySource ActivitySource => _activitySource ??= new ActivitySource(TelemetryLabels.ScopeName);
    public Meter Meter => _meter ??= new Meter(TelemetryLabels.ScopeName);

    public Histogram<double>? CreateCommandDurationHistogram(IEnumerable<KeyValuePair<string, object?>>? tags)
    {
        return Meter.CreateHistogram<double>(name: TelemetryLabels.InstrumentCommandDurationName, unit: "s",
            description: "Duration of the command", tags: tags);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _activitySource?.Dispose();
        _meter?.Dispose();
    }
}
