using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConsoleTables;
using Kiota.Builder;
using Kiota.Builder.SearchProviders;
using Microsoft.Extensions.Logging;

namespace kiota;

internal class KiotaSearchCommandHandler : BaseKiotaCommandHandler
{
    public Argument<string> SearchTermArgument { get; set; }
    public Option<bool> ClearCacheOption { get; set; }
    public Option<string> VersionOption { get; set; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string searchTerm = context.ParseResult.GetValueForArgument(SearchTermArgument);
        string version = context.ParseResult.GetValueForOption(VersionOption);
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));

        Configuration.Search.SearchTerm = searchTerm;
        Configuration.Search.Version = version;


        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaSearcher>(context);
        using (loggerFactory) {
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration));

            try {
                var results = await new KiotaSearcher(logger, Configuration.Search).SearchAsync(cancellationToken);
                DisplayResults(results);
                return 0;
            } catch (Exception ex) {
    #if DEBUG
                logger.LogCritical(ex, "error searching for a description: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
    #else
                logger.LogCritical("error searching for a description: {exceptionMessage}", ex.Message);
                return 1;
    #endif
            }
        }
    }
    private static void DisplayResults(IDictionary<string, SearchResult> results){
        if (results.Count == 1) {
            var result = results.First();
            Console.WriteLine($"Key: {result.Key}");
            Console.WriteLine($"Title: {result.Value.Title}");
            Console.WriteLine($"Description: {result.Value.Description}");
            Console.WriteLine($"Service: {result.Value.ServiceUrl}");
            Console.WriteLine($"OpenAPI: {result.Value.DescriptionUrl}");
        }  else {
            var table = new ConsoleTable("key", "title", "description", "versions");
            Console.WriteLine();
            foreach (var result in results) {
                table.AddRow(result.Key, result.Value.Title, ShortenDescription(result.Value.Description), string.Join(", ", result.Value.VersionLabels));
            }
            table.Write();
            Console.WriteLine();
            Console.WriteLine("multiple matches found, use the key to select a specific description");
        }
    }
    private const int MaxDescriptionLength = 70;
    private static string ShortenDescription(string description) {
        if (string.IsNullOrEmpty(description))
            return string.Empty;
        if (description.Length > MaxDescriptionLength)
            return description[..MaxDescriptionLength] + "...";
        return description;
    }
}
