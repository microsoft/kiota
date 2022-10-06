using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal abstract class KiotaSearchBasedCommandHandler : BaseKiotaCommandHandler {
    protected async Task<(string, int?)> GetDescriptionFromSearch(string openapi, string searchTerm, ILoggerFactory loggerFactory, ILogger parentLogger, CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(openapi) && !string.IsNullOrEmpty(searchTerm))
        {
            parentLogger.LogInformation("Searching for {searchTerm} in the OpenAPI description repository", searchTerm);
            var searcher = new KiotaSearcher(loggerFactory.CreateLogger<KiotaSearcher>(), Configuration.Search);
            var results = await searcher.SearchAsync(cancellationToken);
            if (results.Count == 1)
                return (results.First().Value.DescriptionUrl.ToString(), null);
            else if(!results.Any()) {
                Console.WriteLine("No results found for the search term, use the search command to locate the description");
                return (string.Empty, 1);
            } else {
                Console.WriteLine("Multiple results found for the search term, use the search command to locate the description");
                return (string.Empty, 1);
            }
        }
        return (GetAbsolutePath(openapi), null);
    }
}
