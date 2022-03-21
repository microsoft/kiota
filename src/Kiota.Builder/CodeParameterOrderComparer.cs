using System.Collections.Generic;

namespace Kiota.Builder;
public class CodeParameterOrderComparer : IComparer<CodeParameter>
{
    public int Compare(CodeParameter x, CodeParameter y)
    {
        return (x, y) switch {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => x.Optional.CompareTo(y.Optional) * optionalWeight +
                getKindOrderHint(x.Kind).CompareTo(getKindOrderHint(y.Kind)) * kindWeight +
                x.Name.CompareTo(y.Name) * nameWeight,
        };
    }
    private static int getKindOrderHint(CodeParameterKind kind) {
        return kind switch {
            CodeParameterKind.PathParameters => 1,
            CodeParameterKind.RawUrl => 2,
            CodeParameterKind.RequestAdapter => 3,
            CodeParameterKind.Path => 4,
            CodeParameterKind.QueryParameter => 5,
            CodeParameterKind.RequestBody => 6,
            CodeParameterKind.Headers => 7,
            CodeParameterKind.Options => 8,
            CodeParameterKind.ResponseHandler => 9,
            CodeParameterKind.Serializer => 10,
            CodeParameterKind.BackingStore => 11,
            CodeParameterKind.SetterValue => 12,
            CodeParameterKind.ParseNode => 13,
            CodeParameterKind.Custom => 14,
            _ => 15,
        };
    }
    private static readonly int optionalWeight = 10000;
    private static readonly int kindWeight = 100;
    private static readonly int nameWeight = 10;
}
