using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.CodeDOM;

public class CodeConstant : CodeTerminalWithKind<CodeConstantKind>
{
    public CodeElement? OriginalCodeElement
    {
        get;
        set;
    }
    public static CodeConstant? FromQueryParametersMapping(CodeInterface source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Kind is not CodeInterfaceKind.QueryParameters) throw new InvalidOperationException("Cannot create a query parameters constant from a non query parameters interface");
        if (!source.Properties.Any(static x => !string.IsNullOrEmpty(x.SerializationName))) return default;
        return new CodeConstant
        {
            Name = $"{source.Name.ToFirstCharacterLowerCase()}Mapper",
            Kind = CodeConstantKind.QueryParametersMapper,
            OriginalCodeElement = source,
        };
    }
    public static CodeConstant? FromCodeEnum(CodeEnum source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new CodeConstant
        {
            Name = $"{source.Name.ToFirstCharacterLowerCase()}Object",
            Kind = CodeConstantKind.EnumObject,
            OriginalCodeElement = source,
        };
    }
}
public enum CodeConstantKind
{
    QueryParametersMapper,
    EnumObject,
}
