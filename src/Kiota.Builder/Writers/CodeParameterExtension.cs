using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

public static class CodeParameterExtensions
{
    public static CodeProperty? GetHeadersProperty(this CodeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.Type is CodeType type &&
                type.TypeDefinition is CodeClass cls &&
                cls.IsOfKind(CodeClassKind.RequestConfiguration) &&
                cls.Properties.FirstOrDefault(p => p.IsOfKind(CodePropertyKind.Headers)) is CodeProperty headersProperty ?
                    headersProperty :
                    default;
    }
    public static CodeProperty? GetQueryProperty(this CodeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.Type is CodeType type &&
                type.TypeDefinition is CodeClass cls &&
                cls.IsOfKind(CodeClassKind.RequestConfiguration) &&
                cls.Properties.FirstOrDefault(p => p.IsOfKind(CodePropertyKind.QueryParameters)) is CodeProperty queryProperty ?
                    queryProperty :
                    default;
    }
    public static CodeProperty? GetOptionsProperty(this CodeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.Type is CodeType type &&
                type.TypeDefinition is CodeClass cls &&
                cls.IsOfKind(CodeClassKind.RequestConfiguration) &&
                cls.Properties.FirstOrDefault(p => p.IsOfKind(CodePropertyKind.Options)) is CodeProperty optionsProperty ?
                    optionsProperty :
                    default;
    }
}
