using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Go;

public class GoCodeParameterOrderComparer : BaseCodeParameterOrderComparer
{
    // Cancellation/context parameters must come before other parameters with defaults in Golang.
    protected override int GetKindOrderHint(CodeParameterKind kind)
    {
        return kind switch
        {
            CodeParameterKind.Cancellation => 0,
            CodeParameterKind.PathParameters => 1,
            CodeParameterKind.RawUrl => 2,
            CodeParameterKind.RequestAdapter => 3,
            CodeParameterKind.Path => 4,
            CodeParameterKind.RequestConfiguration => 5,
            CodeParameterKind.RequestBody => 6,
            CodeParameterKind.Serializer => 7,
            CodeParameterKind.BackingStore => 8,
            CodeParameterKind.SetterValue => 9,
            CodeParameterKind.ParseNode => 10,
            CodeParameterKind.Custom => 11,
            _ => 13,
        };
    }
}
