using System.IO.Pipes;
using Nerdbank.Streams;
using StreamJsonRpc;
namespace Kiota.JsonRpcServer;
class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && "stdio".Equals(args[0], StringComparison.OrdinalIgnoreCase))
        {
            await using var stream = FullDuplexStream.Splice(Console.OpenStandardInput(), Console.OpenStandardOutput());
            await RespondToRpcRequestsAsync(stream, 0);
        }
        else
        {
            await NamedPipeServerAsync();
        }

        return 0;
    }

    private static async Task NamedPipeServerAsync()
    {
        int clientId = 0;
        while (true)
        {
            await Console.Error.WriteLineAsync("Waiting for client to make a connection...");
            var stream = new NamedPipeServerStream("StreamJsonRpcKiotaPipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await stream.WaitForConnectionAsync();
            RespondToRpcRequestsAsync(stream, ++clientId);
        }
    }

    private static async Task RespondToRpcRequestsAsync(Stream stream, int clientId)
    {
        await Console.Error.WriteLineAsync($"Connection request #{clientId} received. Spinning off an async Task to cater to requests.");
        var jsonRpc = JsonRpc.Attach(stream, new Server());
        await Console.Error.WriteLineAsync($"JSON-RPC listener attached to #{clientId}. Waiting for requests...");
        await jsonRpc.Completion;
        await Console.Error.WriteLineAsync($"Connection #{clientId} terminated.");
    }
}
