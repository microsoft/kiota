using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace kiota.Handlers.Config;

internal class InitHandler : BaseKiotaCommandHandler
{
    public override Task<int> InvokeAsync(InvocationContext context)
    {
        CancellationToken cancellationToken = context.BindingContext.GetService(typeof(CancellationToken)) is CancellationToken token ? token : CancellationToken.None;

        throw new System.NotImplementedException();
    }
}
