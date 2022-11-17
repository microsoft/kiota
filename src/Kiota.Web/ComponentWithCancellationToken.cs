using Microsoft.AspNetCore.Components;

namespace Kiota.Web;

public abstract class ComponentWithCancellationToken : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected CancellationToken ComponentDetached => _cancellationTokenSource.Token;

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    public virtual void Dispose(bool disposing)
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
