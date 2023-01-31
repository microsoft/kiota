﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.SearchProviders;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers;

internal class KiotaSearchCommandHandler : BaseKiotaCommandHandler
{
    public required Argument<string> SearchTermArgument
    {
        get; init;
    }
    public required Option<bool> ClearCacheOption
    {
        get; init;
    }
    public required Option<string> VersionOption
    {
        get; init;
    }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string searchTerm = context.ParseResult.GetValueForArgument(SearchTermArgument);
        string version = context.ParseResult.GetValueForOption(VersionOption) ?? string.Empty;
        bool clearCache = context.ParseResult.GetValueForOption(ClearCacheOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        Configuration.Search.ClearCache = clearCache;


        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaSearcher>(context);
        using (loggerFactory)
        {
            logger.LogTrace("configuration: {configuration}", JsonSerializer.Serialize(Configuration));

            try
            {
                var searcher = await GetKiotaSearcherAsync(loggerFactory, cancellationToken).ConfigureAwait(false);
                var results = await searcher.SearchAsync(searchTerm, version, cancellationToken);
                await DisplayResults(searchTerm, version, results, logger, cancellationToken);
                return 0;
            }
            catch (Exception ex)
            {
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
    private async Task DisplayResults(string searchTerm, string version, IDictionary<string, SearchResult> results, ILogger logger, CancellationToken cancellationToken)
    {
        if (results.Any() && !string.IsNullOrEmpty(searchTerm) && searchTerm.Contains(KiotaSearcher.ProviderSeparator) && results.ContainsKey(searchTerm))
        {
            var result = results.First();
            DisplayInfo($"Key: {result.Key}");
            DisplayInfo($"Title: {result.Value.Title}");
            DisplayInfo($"Description: {result.Value.Description}");
            DisplayInfo($"Service: {result.Value.ServiceUrl}");
            DisplayInfo($"OpenAPI: {result.Value.DescriptionUrl}");
            DisplayDownloadHint(searchTerm, version);
            DisplayShowHint(searchTerm, version);
        }
        else
        {
            var view = new TableView<KeyValuePair<string, SearchResult>>()
            {
                Items = results.OrderBy(static x => x.Key).Select(static x => x).ToList(),
            };
            view.AddColumn(static x => x.Key, "Key");
            view.AddColumn(static x => x.Value.Title, "Title");
            view.AddColumn(static x => ShortenDescription(x.Value.Description), "Description");
            view.AddColumn(static x => string.Join(", ", x.Value.VersionLabels), "Versions");
            var console = new SystemConsole();
            using var terminal = new SystemConsoleTerminal(console);
            var layout = new StackLayoutView { view };
            console.Append(layout);
            DisplaySearchHint(results.Keys.FirstOrDefault(), version);
            await DisplayLoginHint(logger, cancellationToken);
            DisplaySearchAddHint();
        }
    }
    private const int MaxDescriptionLength = 70;
    private static string ShortenDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;
        if (description.Length > MaxDescriptionLength)
            return description[..MaxDescriptionLength] + "...";
        return description;
    }
}
