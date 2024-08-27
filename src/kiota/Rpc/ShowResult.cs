namespace kiota.Rpc;

public record PathItem(string path, string segment, PathItem[] children, bool selected, bool isOperation = false, Uri? documentationUrl = null);

public record ShowResult(List<LogEntry> logs, PathItem? rootNode, string? apiTitle);
