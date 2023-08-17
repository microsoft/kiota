using Kiota.Builder.CodeDOM;

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
            CodeParameterKind.Serializer => 7,
            CodeParameterKind.BackingStore => 8,
            CodeParameterKind.SetterValue => 9,
            CodeParameterKind.ParseNode => 10,
            CodeParameterKind.Custom => 11,
            _ => 13,
        };
    }
}
