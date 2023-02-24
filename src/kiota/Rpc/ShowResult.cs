using System.Collections.Generic;

namespace kiota.Rpc;

public record PathItem(string path, string segment, PathItem[] children);

public record ShowResult(List<LogEntry> logs, PathItem? rootNode);
