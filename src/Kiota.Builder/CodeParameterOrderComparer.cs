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
            CodeParameterKind.RequestConfiguration => 4,
            CodeParameterKind.QueryParameter => 6,
            CodeParameterKind.RequestBody => 7,
            CodeParameterKind.Headers => 8,
            CodeParameterKind.Options => 9,
            CodeParameterKind.ResponseHandler => 10,
            CodeParameterKind.Serializer => 11,
            CodeParameterKind.BackingStore => 12,
            CodeParameterKind.SetterValue => 13,
            CodeParameterKind.ParseNode => 14,
            CodeParameterKind.Custom => 15,
            _ => 16,
        };
    }
    private static readonly int optionalWeight = 10000;
    private static readonly int kindWeight = 100;
    private static readonly int nameWeight = 10;
}
