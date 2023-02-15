using Microsoft.Extensions.Logging;
namespace Kiota.JsonRpcServer;
public record LogEntry(
    LogLevel level,
    string message
);
