using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, TypeScriptConventionService>
{
    public CodeMethodWriter(TypeScriptConventionService conventionService) : base(conventionService)
    {
    }
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is CodeFunction) return;

        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var isVoid = "void".EqualsIgnoreCase(returnType);
        WriteMethodDocumentation(codeElement, writer, isVoid);
        WriteMethodPrototype(codeElement, writer, returnType, isVoid);
        if (codeElement.Parent is CodeClass)
            throw new InvalidOperationException("No method implementations are generated in typescript: either functions or constants.");
    }

    private void WriteMethodDocumentation(CodeMethod code, LanguageWriter writer, bool isVoid)
    {
        var returnRemark = (isVoid, code.IsAsync) switch
        {
            (true, _) => string.Empty,
            (false, true) => $"@returns a Promise of {code.ReturnType.Name.ToFirstCharacterUpperCase()}",
            (false, false) => $"@returns a {code.ReturnType.Name}",
        };
        conventions.WriteLongDescription(code,
                                        writer,
                                        code.Parameters
                                            .Where(static x => x.Documentation.DescriptionAvailable)
                                            .OrderBy(static x => x.Name)
                                            .Select(x => $"@param {x.Name} {TypeScriptConventionService.RemoveInvalidDescriptionCharacters(x.Documentation.Description)}")
                                            .Union(new[] { returnRemark }));
    }
    private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid)
    {
        WriteMethodPrototypeInternal(code, writer, returnType, isVoid, conventions, false);
    }
    internal static void WriteMethodPrototypeInternal(CodeMethod code, LanguageWriter writer, string returnType, bool isVoid, TypeScriptConventionService pConventions, bool isFunction)
    {
        var accessModifier = isFunction || code.Parent is CodeInterface ? string.Empty : pConventions.GetAccessModifier(code.Access);
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor);
        var methodName = (code.Kind switch
        {
            _ when code.IsAccessor => code.AccessedProperty?.Name,
            _ => code.Name,
        })?.ToFirstCharacterLowerCase();
        var asyncPrefix = code.IsAsync && code.Kind != CodeMethodKind.RequestExecutor ? " async " : string.Empty;
        var staticPrefix = code.IsStatic && !isFunction ? "static " : string.Empty;
        var functionPrefix = isFunction ? "export function " : " ";
        var parameters = string.Join(", ", code.Parameters.Order(parameterOrderComparer).Select(p => pConventions.GetParameterSignature(p, code)));
        var asyncReturnTypePrefix = code.IsAsync ? "Promise<" : string.Empty;
        var asyncReturnTypeSuffix = code.IsAsync ? ">" : string.Empty;
        var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? " | undefined" : string.Empty;
        var accessorPrefix = code.Kind switch
        {
            CodeMethodKind.Getter => "get ",
            CodeMethodKind.Setter => "set ",
            _ => string.Empty
        };
        var shouldHaveTypeSuffix = !code.IsAccessor && !isConstructor && !string.IsNullOrEmpty(returnType);
        var returnTypeSuffix = shouldHaveTypeSuffix ? $" : {asyncReturnTypePrefix}{returnType}{nullableSuffix}{asyncReturnTypeSuffix}" : string.Empty;
        var openBracketSuffix = code.Parent is CodeClass || isFunction ? " {" : ";";
        writer.WriteLine($"{accessModifier}{functionPrefix}{accessorPrefix}{staticPrefix}{methodName}{asyncPrefix}({parameters}){returnTypeSuffix}{openBracketSuffix}");
    }
}
