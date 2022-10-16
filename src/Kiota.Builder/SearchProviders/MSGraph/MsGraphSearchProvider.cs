using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.SearchProviders.MSGraph;
public class MSGraphSearchProvider : ISearchProvider
{
    public string ProviderKey => "msgraph";
    public HashSet<string> KeysToExclude { get; set; }
    public Task<IDictionary<string, SearchResult>> SearchAsync(string term, string version, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(term))
            throw new ArgumentNullException(nameof(term));

        if (string.IsNullOrEmpty(version))
            version = "v1.0";
        else
            version = version.ToLowerInvariant();

        if(AcceptedVersions.Contains(version) && term.Split(new char[] {' ', '-'}, StringSplitOptions.RemoveEmptyEntries).Any(x => Keywords.Contains(x))) {
            return Task.FromResult<IDictionary<string, SearchResult>>(new Dictionary<string, SearchResult> {
                { "microsoft-graph", new SearchResult(ApiTitle, ApiDescription, new Uri($"https://graph.microsoft.com/{version}"), new Uri($"https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/{version}/openapi.yaml"), new List<string> { "v1.0", "beta" }) }
            });
        }
        
        return Task.FromResult<IDictionary<string, SearchResult>>(new Dictionary<string, SearchResult>());
    }
    private readonly HashSet<string> AcceptedVersions = new(StringComparer.OrdinalIgnoreCase) { "v1.0", "beta" };
    private const string ApiTitle = "Microsoft Graph";
    private const string ApiDescription = "Microsoft Graph is a unified API endpoint that enables developers to integrate with the data and intelligence in Microsoft 365, Windows 10, and Enterprise Mobility + Security.";
    private readonly HashSet<string> Keywords = new (20, StringComparer.OrdinalIgnoreCase) {
        "microsoft",
        "graph",
        "msgraph",
        "microsoftgraph",
        "office365",
        "microsoft365",
        "intune",
        "exchange",
        "sharepoint",
        "onedrive",
        "teams",
        "outlook",
        "active",
        "directory",
        "planner",
        "todo",
        "excel",
        "word",
        "powerpoint",
        "onenote"
    };
}
