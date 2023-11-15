using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public class CodeConstant : CodeTerminalWithKind<CodeConstantKind>
{
    public CodeInterface? OriginalInterface
    {
        get;
        set;
    }
    public static CodeConstant FromQueryParametersMapping(CodeInterface source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Kind is not CodeInterfaceKind.QueryParameters) throw new InvalidOperationException("Cannot create a query parameters constant from a non query parameters interface");
        return new CodeConstant
        {
            Name = $"{source.Name.ToFirstCharacterLowerCase()}Mapper",
            Kind = CodeConstantKind.QueryParametersMapper,
            OriginalInterface = source,
        };
    }
}
public enum CodeConstantKind
{
    QueryParametersMapper,
}
