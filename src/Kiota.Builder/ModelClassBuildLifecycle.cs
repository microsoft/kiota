using System;
using System.Threading;

namespace Kiota.Builder;

public class ModelClassBuildLifecycle : IDisposable
{
    private readonly CountdownEvent propertiesBuilt = new(1);
    public ModelClassBuildLifecycle()
    {
    }
    public Boolean IsPropertiesBuilt()
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
        if (!IsPropertiesBuilt())
        {
            if (!propertiesBuilt.Signal())
            {
                throw new InvalidOperationException("PropertiesBuilt CountdownEvent is expected to always reach 0 at this point.");
            }
        }
        Monitor.Exit(propertiesBuilt);
    }
    private bool isDisposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (isDisposed) return;

        if (disposing)
        {
            // free managed resources
            propertiesBuilt.Dispose();
        }

        isDisposed = true;
    }
}
