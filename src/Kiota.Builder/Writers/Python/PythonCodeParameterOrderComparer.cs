using Kiota.Builder.CodeDOM;
using Kiota.Builder.OrderComparers;

namespace Kiota.Builder.Writers.Python;

public class PythonCodeParameterOrderComparer : BaseCodeParameterOrderComparer
{
    // Non-default parameters must come before parameters with defaults in python.
    protected override int GetKindOrderHint(CodeParameterKind kind)
    {
        return kind switch
        {
            CodeParameterKind.RequestAdapter => 1,
            CodeParameterKind.RawUrl => 2,
            CodeParameterKind.PathParameters => 3,
            CodeParameterKind.Path => 4,
            CodeParameterKind.RequestConfiguration => 5,
            CodeParameterKind.RequestBody => 6,
            CodeParameterKind.RequestBodyContentType => 7,
            CodeParameterKind.Serializer => 8,
            CodeParameterKind.BackingStore => 9,
            CodeParameterKind.SetterValue => 10,
            CodeParameterKind.ParseNode => 11,
            CodeParameterKind.Custom => 12,
            _ => 13,
        };
    }
}
