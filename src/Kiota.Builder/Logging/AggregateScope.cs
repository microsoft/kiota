using System;

namespace Kiota.Builder.Logging;

internal class AggregateScope : IDisposable
{
    private readonly IDisposable[] _scopes;
    public AggregateScope(params IDisposable[] scopes)
    {
        _scopes = scopes;
    }
    public void Dispose()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        GC.SuppressFinalize(this);
    }
}
