using System.Collections.Generic;

namespace Kiota.Builder;
public class CodeParameterOrderComparer : IComparer<CodeParameter>
{

    private readonly List<string> parameterTypeOrders;

    public CodeParameterOrderComparer(List<string> defaultParamOrder)
    {
        parameterTypeOrders = defaultParamOrder;
    }
    public CodeParameterOrderComparer()
    {
        parameterTypeOrders = new List<string>();
    }

    public int Compare(CodeParameter x, CodeParameter y)
    {
        return (x, y) switch {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => getDefaultOrder(y.Type).CompareTo(getDefaultOrder(x.Type)) * defaultParamOrderWeight +
                 x.Optional.CompareTo(y.Optional) * optionalWeight +
                 getKindOrderHint(x.Kind).CompareTo(getKindOrderHint(y.Kind)) * kindWeight +
                 x.Name.CompareTo(y.Name) * nameWeight,
        };
    }
    
    private int getDefaultOrder(CodeTypeBase codeType)
    {
        return parameterTypeOrders.IndexOf(codeType.Name);
    }
    private static int getKindOrderHint(CodeParameterKind kind) {
        return kind switch {
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
    private static readonly int defaultParamOrderWeight = 100000;
    private static readonly int optionalWeight = 10000;
    private static readonly int kindWeight = 100;
    private static readonly int nameWeight = 10;
}
