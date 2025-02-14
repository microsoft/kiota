using System.Runtime.CompilerServices;

namespace kiota.Telemetry;

public class Telemetry
{
    internal static KeyValuePair<string, object?>[] GetThreadTags()
    {
        var name = Thread.CurrentThread.Name;
        var id = Environment.CurrentManagedThreadId;

        if (name is not null)
        {
            return
            [
                new KeyValuePair<string, object?>("thread.id", id),
                new KeyValuePair<string, object?>("thread.name", name)
            ];
        }

        return
        [
            new KeyValuePair<string, object?>("thread.id", id)
        ];
    }
}
