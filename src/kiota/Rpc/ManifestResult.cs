using System.Collections.Generic;

namespace kiota.Rpc;
public record ManifestResult(List<LogEntry> logs, string? apiDescriptionPath, string[]? selectedPaths);
