using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.WorkspaceManagement;
using Microsoft.Extensions.Logging;

namespace kiota.Handlers.Workspace;

internal class MigrateHandler : BaseKiotaCommandHandler
{
    public required Option<string> LockDirectoryOption
    {
        get;
        init;
    }
    public required Option<string> ClassOption
    {
        get; init;
    }
    public override async Task<int> InvokeAsync(InvocationContext context)
    {
        var workingDirectory = NormalizeSlashesInPath(Directory.GetCurrentDirectory());
        string lockDirectory = context.ParseResult.GetValueForOption(LockDirectoryOption) ?? workingDirectory;
        string clientName = context.ParseResult.GetValueForOption(ClassOption) ?? string.Empty;
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        lockDirectory = NormalizeSlashesInPath(lockDirectory);
        var (loggerFactory, logger) = GetLoggerAndFactory<WorkspaceManagementService>(context, $"./{DescriptionStorageService.KiotaDirectorySegment}");
        using (loggerFactory)
        {
            try
            {
                var workspaceManagementService = new WorkspaceManagementService(logger, httpClient, true, workingDirectory);
                var clientNames = await workspaceManagementService.MigrateFromLockFileAsync(clientName, lockDirectory, cancellationToken).ConfigureAwait(false);
                if (!clientNames.Any())
                {
                    DisplayWarning("no client configuration was migrated");
                    return 1;
                }
                DisplaySuccess($"Client configurations migrated successfully: {string.Join(", ", clientNames)}");
                DisplayGenerateAfterMigrateHint();
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "error migrating the workspace configuration");
                return 1;
            }
        }
    }
}
