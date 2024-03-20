using System.CommandLine.Invocation;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Workspace;

internal class InitHandler : BaseKiotaCommandHandler
{
    public async override Task<int> InvokeAsync(InvocationContext context)
    {
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        var workspaceStorageService = new WorkspaceConfigurationStorageService(Directory.GetCurrentDirectory());
        var (loggerFactory, logger) = GetLoggerAndFactory<WorkspaceConfigurationStorageService>(context, Configuration.Generation.OutputPath);
        using (loggerFactory)
        {
            try
            {
                await workspaceStorageService.InitializeAsync(cancellationToken).ConfigureAwait(false);
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "error initializing the workspace configuration");
                return 1;
            }
        }
    }
}
