using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Microsoft.Extensions.Logging;
using Kiota;
using Microsoft.OpenApi.Services;

namespace kiota;
internal class KiotaDisplayCommandHandler : BaseKiotaCommandHandler
{
    public Option<string> DescriptionOption { get;set; }
    public Option<string> SearchTermOption { get; set; }
    public Option<string> VersionOption { get; set; }
    public Option<uint> MaxDepthOption { get; set; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption);
        string searchTerm = context.ParseResult.GetValueForOption(SearchTermOption);
        string version = context.ParseResult.GetValueForOption(VersionOption);
        uint maxDepth = context.ParseResult.GetValueForOption(MaxDepthOption);
        CancellationToken cancellationToken = (CancellationToken)context.BindingContext.GetService(typeof(CancellationToken));

        //TODO filter parameters
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context);

        Configuration.Search.SearchTerm = searchTerm;
        Configuration.Search.Version = version;
        using (loggerFactory) {
            var (searchResultDescription, statusCode) = await GetDescriptionFromSearch(openapi, searchTerm, loggerFactory, logger, cancellationToken);
            if (statusCode.HasValue) {
                return statusCode.Value;
            }
            if (!string.IsNullOrEmpty(searchResultDescription)) {
                openapi = searchResultDescription;
            }
            if (string.IsNullOrEmpty(openapi)) {
                logger.LogError("no description provided");
                return 1;
            }
            Configuration.Generation.OpenAPIFilePath = openapi;
            var urlTreeNode = await new KiotaBuilder(logger, Configuration.Generation).GetUrlTreeNodeAsync(cancellationToken);

            var view = new ConsoleTreeView<OpenApiUrlTreeNode>(urlTreeNode, static x => x.Segment, static x => x.Children.Select(static y => y.Value), maxDepth);
            // new SystemConsole().Append(view);
            Console.Write(view.RenderAsString()); // temporary workaround because commandline rendering trims white spaces
        }
        return 0;
    }
    private async Task<(string, int?)> GetDescriptionFromSearch(string openapi, string searchTerm, ILoggerFactory loggerFactory, ILogger parentLogger, CancellationToken cancellationToken) {
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
        return (string.Empty, null);
    }
}
