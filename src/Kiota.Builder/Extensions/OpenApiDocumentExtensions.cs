using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.EqualityComparers;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;

internal static class OpenApiDocumentExtensions
{
    internal static void InitializeInheritanceIndex(this OpenApiDocument openApiDocument, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
    {
        ArgumentNullException.ThrowIfNull(inheritanceIndex);
        ArgumentNullException.ThrowIfNull(openApiDocument);
        if (inheritanceIndex.IsEmpty && openApiDocument.Components?.Schemas != null)
        {
            Parallel.ForEach(openApiDocument.Components.Schemas, entry =>
            {
                inheritanceIndex.TryAdd(entry.Key, new(StringComparer.OrdinalIgnoreCase));
                if (entry.Value.AllOf != null)
                    foreach (var allOfEntry in entry.Value.AllOf.Where(static x => !string.IsNullOrEmpty(x.Reference?.Id)))
                    {
                        var dependents = inheritanceIndex.GetOrAdd(allOfEntry.Reference.Id, new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase));
                        dependents.TryAdd(entry.Key, false);
                    }
            });
        }
    }
    internal static string? GetAPIRootUrl(this OpenApiDocument openApiDocument, string openAPIFilePath)
    {
        if (openApiDocument == null) return null;
        var candidateUrl = openApiDocument.Servers
                                        .GroupBy(static x => x, new OpenApiServerComparer()) //group by protocol relative urls
                                        .FirstOrDefault()
                                        ?.OrderByDescending(static x => x?.Url, StringComparer.OrdinalIgnoreCase) // prefer https over http
                                        ?.FirstOrDefault()
                                        ?.Url;
        if (string.IsNullOrEmpty(candidateUrl))
            return null;
        else if (!candidateUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                openAPIFilePath.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(openAPIFilePath, new(), out var filePathUri) &&
                Uri.TryCreate(filePathUri, candidateUrl, out var candidateUri))
        {
            candidateUrl = candidateUri.ToString();
        }
        return candidateUrl.TrimEnd(KiotaBuilder.ForwardSlash);
    }
}
