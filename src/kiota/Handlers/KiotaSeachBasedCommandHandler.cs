using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal abstract class KiotaSearchBasedCommandHandler : BaseKiotaCommandHandler
{
    protected async Task<(string, int?)> GetDescriptionFromSearchAsync(string openapi, string manifest, string searchTerm, string version, KiotaConfiguration configuration, ILoggerFactory loggerFactory, HttpClient httpClient, ILogger logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(openapi) && string.IsNullOrEmpty(manifest) && !string.IsNullOrEmpty(searchTerm))
        {
            logger.LogInformation("Searching for {searchTerm} in the OpenAPI description repository", searchTerm);
            var searcher = await GetKiotaSearcherAsync(configuration, loggerFactory, httpClient, cancellationToken).ConfigureAwait(false);
            var results = await searcher.SearchAsync(searchTerm, version, cancellationToken).ConfigureAwait(false);
            if (results.TryGetValue(searchTerm, out var result))
                return (result.DescriptionUrl?.ToString() ?? string.Empty, null);
            else if (results.Count == 1)
                return (results.First().Value.DescriptionUrl?.ToString() ?? string.Empty, null);
            else if (!results.Any())
            {
                DisplayWarning("No results found for the search term, use the search command to locate the description");
                return (string.Empty, 1);
            }
            else
            {
                DisplayWarning("Multiple results found for the search term, use the search command to locate the description");
                return (string.Empty, 1);
            }
        }
        return (GetAbsolutePath(openapi), null);
    }
}
