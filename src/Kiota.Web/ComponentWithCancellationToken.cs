using Microsoft.AspNetCore.Components;

namespace Kiota.Web;

public abstract class ComponentWithCancellationToken : ComponentBase, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;

    protected CancellationToken ComponentDetached => (_cancellationTokenSource ??= new()).Token;

    public virtual void Dispose()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
        GC.SuppressFinalize(this);
    }
}
