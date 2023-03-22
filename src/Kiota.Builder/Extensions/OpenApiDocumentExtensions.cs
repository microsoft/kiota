using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;

internal static class OpenApiDocumentExtensions
{
    internal static void InitializeInheritanceIndex(this OpenApiDocument openApiDocument, ConcurrentDictionary<string, ConcurrentDictionary<string, (int, bool)>> inheritanceIndex)
    {
        ArgumentNullException.ThrowIfNull(inheritanceIndex);
        ArgumentNullException.ThrowIfNull(openApiDocument);
        if (!inheritanceIndex.Any() && openApiDocument.Components?.Schemas != null)
        {
            Parallel.ForEach(openApiDocument.Components.Schemas, entry =>
            {
                inheritanceIndex.TryAdd(entry.Key, new(StringComparer.OrdinalIgnoreCase));
                if (entry.Value.AllOf != null)
                    foreach (var allOfEntry in entry.Value.AllOf.Where(static x => !string.IsNullOrEmpty(x.Reference?.Id)))
                    {
                        var dependents = inheritanceIndex.GetOrAdd(allOfEntry.Reference.Id, new ConcurrentDictionary<string, (int, bool)>(StringComparer.OrdinalIgnoreCase));
                        // TODO: the following should be an atomic operation
                        var max = 0;
                        if (dependents.Any())
                            max = dependents.Values.MaxBy(static x => x.Item1).Item1;
                        dependents.TryAdd(entry.Key, (max + 1, false));
                    }
            });
        }
    }
}
