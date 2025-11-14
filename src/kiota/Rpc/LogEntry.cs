using Microsoft.Extensions.Logging;
namespace kiota.Rpc;

public record LogEntry(
    LogLevel level,
    string message
);
