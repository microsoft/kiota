using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Pipes;
using kiota.Rpc;
using Nerdbank.Streams;
using StreamJsonRpc;
namespace kiota.Handlers;

internal class KiotaRpcCommandHandler : ICommandHandler
{
    public required Option<RpcMode> ModeOption
    {
        get; set;
    }
    public required Option<string> PipeNameOption
    {
        get;
        set;
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var mode = context.ParseResult.GetValueForOption(ModeOption);
        var pipeName = context.ParseResult.GetValueForOption(PipeNameOption);
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;
        if (mode == RpcMode.Stdio)
        {
            await using var stream = FullDuplexStream.Splice(Console.OpenStandardInput(), Console.OpenStandardOutput());
            await RespondToRpcRequestsAsync(stream, 0);
        }
        else if (string.IsNullOrEmpty(pipeName))
        {
            throw new InvalidOperationException("Pipe name must be specified when using named pipe mode.");
        }
        else
        {
            await NamedPipeServerAsync(pipeName, cancellationToken);
        }

        return 0;
    }

    private static async Task NamedPipeServerAsync(string pipeName, CancellationToken cancellationToken)
    {
        int clientId = 0;
        while (true)
        {
            await Console.Error.WriteLineAsync("Waiting for client to make a connection...");
            var stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await stream.WaitForConnectionAsync(cancellationToken);
#pragma warning disable CS4014
            // We don't await this task because we want to keep listening for new connections.
            RespondToRpcRequestsAsync(stream, ++clientId);
#pragma warning restore CS4014
        }
    }

    private static async Task RespondToRpcRequestsAsync(Stream stream, int clientId)
    {
        await Console.Error.WriteLineAsync($"Connection request #{clientId} received. Spinning off an async Task to cater to requests.");
        using var jsonRpc = JsonRpc.Attach(stream, new Server());
        await Console.Error.WriteLineAsync($"JSON-RPC listener attached to #{clientId}. Waiting for requests...");
        await jsonRpc.Completion;
        await Console.Error.WriteLineAsync($"Connection #{clientId} terminated.");
    }

    public int Invoke(InvocationContext context)
    {
        throw new InvalidOperationException("This command handler is only intended to be used with the async entry point.");
    }
}
