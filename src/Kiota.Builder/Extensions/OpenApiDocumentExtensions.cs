using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                    foreach (var allOfEntry in entry.Value.AllOf)
                    {
                        if (string.IsNullOrEmpty(allOfEntry.Reference?.Id)) continue;
                        var dependents = inheritanceIndex.GetOrAdd(allOfEntry.Reference.Id, new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase));
                        dependents.TryAdd(entry.Key, false);
                    }
            });
        }
    }
    internal static string? GetAPIRootUrl(this OpenApiDocument openApiDocument, string openAPIFilePath)
    {
        ArgumentNullException.ThrowIfNull(openApiDocument);

        var comparer = new OpenApiServerComparer();
        var groupedServers = new Dictionary<int, List<OpenApiServer>>();
        foreach (var server in openApiDocument.Servers)
        {
            var key = comparer.GetHashCode(server);
            if (!groupedServers.TryGetValue(key, out var list))
            {
                list = [];
                groupedServers[key] = list;
            }
            list.Add(server);
        }

        int? firstKey = null;
        foreach (var key in groupedServers.Keys)
        {
            firstKey = key;
            break;
        }

        OpenApiServer? preferredServer = null;
        if (firstKey != null)
        {
            var servers = groupedServers[firstKey.Value];
            foreach (var server in servers)
            {
                if (preferredServer == null || StringComparer.OrdinalIgnoreCase.Compare(server.Url, preferredServer.Url) > 0)
                {
                    preferredServer = server;
                }
            }
        }

        var candidateUrl = preferredServer?.Url;

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
