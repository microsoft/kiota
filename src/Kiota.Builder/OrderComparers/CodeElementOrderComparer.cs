using System;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.OrderComparers;

public class CodeElementOrderComparer : BaseStringComparisonComparer<CodeElement>
{
    public override int Compare(CodeElement? x, CodeElement? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ => GetTypeFactor(x).CompareTo(GetTypeFactor(y)) * TypeWeight +
                GetConstantKindFactor(x).CompareTo(GetConstantKindFactor(y)) * constantKindWeight +
#pragma warning disable CA1062
                CompareStrings(x.Name, y.Name, StringComparer.InvariantCultureIgnoreCase) * NameWeight +
#pragma warning restore CA1062
                GetMethodKindFactor(x).CompareTo(GetMethodKindFactor(y)) * methodKindWeight +
                GetParametersFactor(x).CompareTo(GetParametersFactor(y)) * ParametersWeight,
        };
    }
    private const int NameWeight = 100;
    private const int TypeWeight = 10000;
    protected virtual int GetTypeFactor(CodeElement element)
    {
        return element switch
        {
            CodeUsing => 1,
            ClassDeclaration => 2,
            InterfaceDeclaration => 3,
            CodeProperty => 4,
            CodeIndexer => 5,
            CodeMethod => 6,
            CodeClass => 7,
            CodeConstant => 8,
            BlockEnd => 9,
            _ => 0,
        };
    }

    protected virtual int methodKindWeight { get; } = 10;

    protected virtual int GetMethodKindFactor(CodeElement element)
    {
        if (element is CodeMethod method)
            return method.Kind switch
            {
                CodeMethodKind.ClientConstructor => 1,
                CodeMethodKind.Constructor => 2,
                CodeMethodKind.RawUrlConstructor => 3,
                _ => 0,
            };
        return 0;
    }
    protected virtual int constantKindWeight { get; } = 1000;
    protected virtual int GetConstantKindFactor(CodeElement element)
    {
        if (element is CodeConstant constant)
            return constant.Kind switch
            {
                CodeConstantKind.UriTemplate => 1, // in typescript uri templates need to go first before the request metadata constants
                _ => 2,
            };
        return 0;
    }
    private const int ParametersWeight = 1;
    private static int GetParametersFactor(CodeElement element)
    {
        if (element is CodeMethod method && (method.Parameters?.Any() ?? false))
            return method.Parameters.Count();
        return 0;
    }
}
