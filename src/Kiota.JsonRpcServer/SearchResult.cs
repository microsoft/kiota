using Kiota.Builder.SearchProviders;

namespace Kiota.JsonRpcServer;

public record SearchOperationResult(List<LogEntry> logs, IDictionary<string, SearchResult> results);
