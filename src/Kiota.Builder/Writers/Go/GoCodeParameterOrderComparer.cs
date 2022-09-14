using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Go;

public class GoCodeParameterOrderComparer : BaseCodeParameterOrderComparer
{
    // Cancellation/context parameters must come before other parameters with defaults in Golang.
    protected override int GetKindOrderHint(CodeParameterKind kind) {
        return kind switch {
            CodeParameterKind.Cancellation => 0,
            CodeParameterKind.PathParameters => 1,
            CodeParameterKind.RawUrl => 2,
            CodeParameterKind.RequestAdapter => 3,
            CodeParameterKind.Path => 4,
            CodeParameterKind.RequestConfiguration => 5,
            CodeParameterKind.RequestBody => 6,
            CodeParameterKind.ResponseHandler => 7,
            CodeParameterKind.Serializer => 8,
            CodeParameterKind.BackingStore => 9,
            CodeParameterKind.SetterValue => 10,
            CodeParameterKind.ParseNode => 11,
            CodeParameterKind.Custom => 12,
            _ => 13,
        };
    }
}
