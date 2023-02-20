namespace Kiota.JsonRpcServer;

public record PathItem(string path, string segment, PathItem[] children);

public record ShowResult(List<LogEntry> logs, PathItem? rootNode);
