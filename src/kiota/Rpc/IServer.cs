using Kiota.Builder;
using Kiota.Builder.Configuration;

namespace kiota.Rpc;

internal interface IServer
{
    LanguagesInformation Info();
    string GetVersion();
    Task<List<LogEntry>> UpdateAsync(string output, bool cleanOutput, bool clearCache, CancellationToken cancellationToken);
    Task<SearchOperationResult> SearchAsync(string searchTerm, bool clearCache, CancellationToken cancellationToken);
    Task<ShowResult> ShowAsync(string descriptionPath, string[] includeFilters, string[] excludeFilters, bool clearCache, bool includeKiotaValidationRules, CancellationToken cancellationToken);
    Task<ManifestResult> GetManifestDetailsAsync(string manifestPath, string apiIdentifier, bool clearCache, CancellationToken cancellationToken);
    Task<List<LogEntry>> GenerateAsync(string openAPIFilePath, string outputPath, GenerationLanguage language, string[] includePatterns, string[] excludePatterns, string clientClassName, string clientNamespaceName, bool usesBackingStore, bool cleanOutput, bool clearCache, bool excludeBackwardCompatible, string[] disabledValidationRules, string[] serializers, string[] deserializers, string[] structuredMimeTypes, bool includeAdditionalData, ConsumerOperation operation, CancellationToken cancellationToken);
    Task<LanguagesInformation> InfoForDescriptionAsync(string descriptionPath, bool clearCache, CancellationToken cancellationToken);
    Task<List<LogEntry>> GeneratePluginAsync(string openAPIFilePath, string outputPath, PluginType[] pluginTypes, string[] includePatterns, string[] excludePatterns, string clientClassName, bool cleanOutput, bool clearCache, string[] disabledValidationRules, bool? noWorkspace, PluginAuthType? pluginAuthType, string pluginAuthRefid, ConsumerOperation operation, CancellationToken cancellationToken);
    Task<List<LogEntry>> MigrateFromLockFileAsync(string lockDirectoryPath, CancellationToken cancellationToken);
    Task<List<LogEntry>> RemoveClientAsync(string clientName, bool cleanOutput, CancellationToken cancellationToken);
    Task<List<LogEntry>> RemovePluginAsync(string pluginName, bool cleanOutput, CancellationToken cancellationToken);
    Task<ShowPluginResult> ShowPluginAsync(string descriptionPath, CancellationToken cancellationToken);
}
