using System;
using System.Threading;

namespace Kiota.Builder;

internal sealed class ModelClassBuildLifecycle : IDisposable
{
    private readonly CountdownEvent propertiesBuilt = new(1);
    public bool IsPropertiesBuilt()
    {
        return propertiesBuilt.IsSet;
    }
    public void WaitForPropertiesBuilt()
    {
        if (!Monitor.IsEntered(propertiesBuilt))
        {
            propertiesBuilt.Wait();
        }
    }
    public void StartBuildingProperties()
    {
        Monitor.Enter(propertiesBuilt);
    }
    public void PropertiesBuildingDone()
    {
        if (!IsPropertiesBuilt() && !propertiesBuilt.Signal())
        {
            throw new InvalidOperationException("PropertiesBuilt CountdownEvent is expected to always reach 0 at this point.");
        }
        Monitor.Exit(propertiesBuilt);
    }
    public void Dispose()
    {
        propertiesBuilt.Dispose();
        GC.SuppressFinalize(this);
    }
}
