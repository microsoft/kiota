using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Client;
internal class RemoveHandler : BaseKiotaCommandHandler
{
    public required Option<string> ClassOption
    {
        get; init;
    }
    public required Option<bool> CleanOutputOption
    {
        get; init;
    }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        string className = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        bool cleanOutput = context.ParseResult.GetValueForOption(CleanOutputOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        var (loggerFactory, logger) = GetLoggerAndFactory<KiotaBuilder>(context, $"./{DescriptionStorageService.KiotaDirectorySegment}");
        using (loggerFactory)
        {
            try
            {
                await CheckForNewVersionAsync(logger, cancellationToken).ConfigureAwait(false);
                var workspaceManagementService = new WorkspaceManagementService(logger, true);
                await workspaceManagementService.RemoveClientAsync(className, cleanOutput, cancellationToken).ConfigureAwait(false);
                return 0;
            }
            catch (Exception ex)
            {
#if DEBUG
                logger.LogCritical(ex, "error removing the client: {exceptionMessage}", ex.Message);
                throw; // so debug tools go straight to the source of the exception when attached
#else
                logger.LogCritical("error removing the client: {exceptionMessage}", ex.Message);
                return 1;
#endif
            }
        }
    }
}
