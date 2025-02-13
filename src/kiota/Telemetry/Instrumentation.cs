using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace kiota.Telemetry;

internal class Instrumentation : IDisposable
{
    private readonly Meter _meter = new(TelemetryLabels.ScopeName);

    /// <summary>
    /// An activity source is used to create activities (spans) during manual instrumentation.
    /// </summary>
    public ActivitySource ActivitySource { get; } = new(TelemetryLabels.ScopeName);

    /// <summary>
    /// Creates a histogram instrument used to time command duration
    /// </summary>
    /// <returns>A histogram instrument</returns>
    public Histogram<double> CreateCommandDurationHistogram()
    {
        return _meter.CreateHistogram<double>(name: TelemetryLabels.InstrumentCommandDurationName, unit: "s",
            description: "Duration of the command");
    }

    /// <summary>
    /// Creates a counter instrument for language generation
    /// </summary>
    /// <returns>A counter instrument</returns>
    public Counter<long> CreateLanguageGenerationCounter()
    {
        return _meter.CreateCounter<long>(name: TelemetryLabels.InstrumentGenerationCount,
            description: "Count of generations that have been run");
    }

    /// <summary>
    /// Creates a counter instrument for command execution
    /// </summary>
    /// <returns>A counter instrument</returns>
    public Counter<long> CreateCommandExecutionCounter()
    {
        return _meter.CreateCounter<long>(name: TelemetryLabels.InstrumentCommandExecutionsCount,
            description: "Count of commands that have been run");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.ActivitySource.Dispose();
        this._meter.Dispose();
    }
}
