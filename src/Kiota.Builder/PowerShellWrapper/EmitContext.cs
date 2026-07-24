namespace Kiota.Builder.PowerShellWrapper;

// The per-module values CmdletEmitter's templates need, so the emitter stays module-agnostic.
// ClientNamespace is whatever --namespace-name the module was generated with, for example
// "Microsoft.Graph.PowerShell.Mail.Client".
public sealed record EmitContext(string ClientNamespace, string CmdletNamespace = "MgPoC")
{
    public string ModelsNamespace => $"{ClientNamespace}.Models";
}
