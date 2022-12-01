using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal abstract class KiotaSearchBasedCommandHandler : BaseKiotaCommandHandler {
    protected async Task<(string, int?)> GetDescriptionFromSearch(string openapi, string searchTerm, string version, ILoggerFactory loggerFactory, ILogger logger, CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(openapi) && !string.IsNullOrEmpty(searchTerm))
        {
            logger.LogInformation("Searching for {searchTerm} in the OpenAPI description repository", searchTerm);
            var searcher = await GetKiotaSearcherAsync(loggerFactory, cancellationToken).ConfigureAwait(false);
            var results = await searcher.SearchAsync(searchTerm, version, cancellationToken).ConfigureAwait(false);
            if (results.Count == 1)
                return (results.First().Value.DescriptionUrl.ToString(), null);
            else if(!results.Any()) {
                DisplayWarning("No results found for the search term, use the search command to locate the description");
                return (string.Empty, 1);
            } else {
                DisplayWarning("Multiple results found for the search term, use the search command to locate the description");
                return (string.Empty, 1);
            }
        }
        return (GetAbsolutePath(openapi), null);
    }
}
