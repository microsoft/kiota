using System.Collections.Generic;
using Kiota.Builder.SearchProviders;

namespace kiota.Rpc;

public record SearchOperationResult(List<LogEntry> logs, IDictionary<string, SearchResult> results);
