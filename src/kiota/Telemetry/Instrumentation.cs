using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace kiota.Telemetry;

internal class Instrumentation(IMeterFactory meterFactory) : IDisposable
{
    private readonly Meter _meter = meterFactory.Create(TelemetryLabels.ScopeName);

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
    /// Creates a counter instrument for client generations
    /// </summary>
    /// <returns>A counter instrument</returns>
    public Counter<long> CreateClientGenerationCounter()
    {
        return _meter.CreateCounter<long>(name: TelemetryLabels.InstrumentClientGenerationCount,
            description: "Count of client generations that have been run");
    }

    /// <summary>
    /// Creates a counter instrument for plugin generations
    /// </summary>
    /// <returns>A counter instrument</returns>
    public Counter<long> CreatePluginGenerationCounter()
    {
        return _meter.CreateCounter<long>(name: TelemetryLabels.InstrumentPluginGenerationCount,
            description: "Count of plugin generations that have been run");
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
    }
}
