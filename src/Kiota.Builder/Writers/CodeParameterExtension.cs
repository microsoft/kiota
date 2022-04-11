using System.Linq;

namespace Kiota.Builder.Writers;

public static class CodeParameterExtensions {
    public static CodeProperty GetHeadersProperty(this CodeParameter parameter) {
        return parameter?.Type is CodeType type &&
                type.TypeDefinition is CodeClass cls &&
                cls.IsOfKind(CodeClassKind.RequestConfiguration) &&
                cls.Properties.FirstOrDefault(p => p.IsOfKind(CodePropertyKind.Headers)) is CodeProperty headersProperty ?
                    headersProperty :
                    default;
    }
}
