using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;

namespace kiota.Rpc;
internal interface IServer
{
    string GetVersion();
    Task<List<LogEntry>> UpdateAsync(string output, CancellationToken cancellationToken);
    Task<SearchOperationResult> SearchAsync(string searchTerm, CancellationToken cancellationToken);
    Task<ShowResult> ShowAsync(string descriptionPath, string[] includeFilters, string[] excludeFilters, CancellationToken cancellationToken);
    Task<List<LogEntry>> GenerateAsync(string descriptionPath, string output, GenerationLanguage language, string[] includeFilters, string[] excludeFilters, string clientClassName, string clientNamespaceName, CancellationToken cancellationToken);
}
