using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;
using System.Collections.Generic;
using System.Text;

namespace kiota.Handlers;
internal class KiotaShowCommandHandler : KiotaSearchBasedCommandHandler
{
    public required Option<string> DescriptionOption { get;init; }
    public required Option<string> SearchTermOption { get; init; }
    public required Option<string> VersionOption { get; init; }
    public required Option<uint> MaxDepthOption { get; init; }
    public required Option<List<string>> IncludePatternsOption { get; init; }
    public required Option<List<string>> ExcludePatternsOption { get; init; }
    public required Option<bool> ClearCacheOption { get; init; }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string openapi = context.ParseResult.GetValueForOption(DescriptionOption) ?? string.Empty;
        string searchTerm = context.ParseResult.GetValueForOption(SearchTermOption) ?? string.Empty;
        string version = context.ParseResult.GetValueForOption(VersionOption) ?? string.Empty;
        uint maxDepth = context.ParseResult.GetValueForOption(MaxDepthOption);
        List<string> includePatterns = context.ParseResult.GetValueForOption(IncludePatternsOption) ?? new List<string>();
        List<string> excludePatterns = context.ParseResult.GetValueForOption(ExcludePatternsOption) ?? new List<string>();
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context);

        Configuration.Search.ClearCache = clearCache;
        using (loggerFactory) {
            var descriptionProvided = !string.IsNullOrEmpty(openapi) && string.IsNullOrEmpty(searchTerm);
            var (searchResultDescription, statusCode) = await GetDescriptionFromSearch(openapi, searchTerm, version, loggerFactory, logger, cancellationToken);
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
            Configuration.Generation.IncludePatterns = includePatterns.ToHashSet();
            Configuration.Generation.ExcludePatterns = excludePatterns.ToHashSet();
            Configuration.Generation.ClearCache = clearCache;
            try {
                var urlTreeNode = await new KiotaBuilder(logger, Configuration.Generation, httpClient).GetUrlTreeNodeAsync(cancellationToken);

                var builder = new StringBuilder();
                RenderNode(urlTreeNode, maxDepth, builder);
                var tree = builder.ToString();
                Console.Write(tree);
                if(descriptionProvided)
                    DisplayShowAdvancedHint(string.Empty, string.Empty, includePatterns, excludePatterns, openapi);
                else
                    DisplayShowAdvancedHint(searchTerm, version, includePatterns, excludePatterns, openapi);
                DisplayGenerateHint(openapi, includePatterns, excludePatterns);
            } catch (Exception ex) {
#if DEBUG
                logger.LogCritical(ex, "error showing the description: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error showing the description: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }

        }
        return 0;
    }
    private const string Cross = " ├─";
    private const string Corner = " └─";
    private const string Vertical = " │ ";
    private const string Space = "   ";
    private static void RenderNode(OpenApiUrlTreeNode node, uint maxDepth, StringBuilder builder, string indent = "", int nodeDepth = 0)
    {
        builder.AppendLine(node.Segment);

        var children = node.Children;
        var numberOfChildren = children.Count;
        for (var i = 0; i < numberOfChildren; i++)
        {
            var child = children.ElementAt(i);
            var isLast = i == (numberOfChildren - 1);
            RenderChildNode(child.Value, maxDepth, builder, indent, isLast, nodeDepth);
        }
    }

    private static void RenderChildNode(OpenApiUrlTreeNode node, uint maxDepth, StringBuilder builder, string indent, bool isLast, int nodeDepth = 0)
    {
        if (nodeDepth >= maxDepth && maxDepth != 0)
            return;
        builder.Append(indent);

        if (isLast)
        {
            builder.Append(Corner);
            indent += Space;
        }
        else
        {
            builder.Append(Cross);
            indent += Vertical;
        }

        RenderNode(node, maxDepth, builder, indent, nodeDepth + 1);
    }
}
