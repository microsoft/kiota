using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder;
using Kiota.Builder.Configuration;

namespace kiota.Rpc;
internal interface IServer
{
    LanguagesInformation Info();
    string GetVersion();
    Task<List<LogEntry>> UpdateAsync(string output, CancellationToken cancellationToken);
    Task<SearchOperationResult> SearchAsync(string searchTerm, CancellationToken cancellationToken);
    Task<ShowResult> ShowAsync(string descriptionPath, string[] includeFilters, string[] excludeFilters, CancellationToken cancellationToken);
    Task<ManifestResult> GetManifestDetailsAsync(string manifestPath, string apiIdentifier, CancellationToken cancellationToken);
    Task<List<LogEntry>> GenerateAsync(string descriptionPath, string output, GenerationLanguage language, string[] includeFilters, string[] excludeFilters, string clientClassName, string clientNamespaceName, CancellationToken cancellationToken);
    Task<LanguagesInformation> InfoForDescriptionAsync(string descriptionPath, CancellationToken cancellationToken);
}
