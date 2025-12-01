using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.OrderComparers;
using static Kiota.Builder.Writers.TypeScript.TypeScriptConventionService;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeMethodWriter(TypeScriptConventionService conventionService) : BaseElementWriter<CodeMethod, TypeScriptConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is CodeFunction) return;

        var codeFile = codeElement.GetImmediateParentOfType<CodeFile>();
        var returnType = GetTypescriptTypeString(codeElement.ReturnType, codeFile, inlineComposedTypeString: true);
        var isVoid = "void".EqualsIgnoreCase(returnType);
        WriteMethodDocumentation(codeFile, codeElement, writer, isVoid);
        WriteMethodPrototype(codeElement, writer, returnType, isVoid);
        if (codeElement.Parent is CodeClass)
            throw new InvalidOperationException("No method implementations are generated in typescript: either functions or constants.");
    }

    private void WriteMethodDocumentation(CodeFile codeFile, CodeMethod code, LanguageWriter writer, bool isVoid)
    {
        WriteMethodDocumentationInternal(codeFile, code, writer, isVoid, conventions);
    }
    internal static void WriteMethodDocumentationInternal(CodeFile codeFile, CodeMethod code, LanguageWriter writer, bool isVoid, TypeScriptConventionService typeScriptConventionService)
    {
        var returnRemark = (isVoid, code.IsAsync) switch
        {
            (true, _) => string.Empty,
            (false, true) => $"@returns {{Promise<{GetTypescriptTypeString(code.ReturnType, code, inlineComposedTypeString: true)}>}}",
            (false, false) => $"@returns {{{GetTypescriptTypeString(code.ReturnType, code, inlineComposedTypeString: true)}}}",
        };
        typeScriptConventionService.WriteLongDescription(code,
                                        writer,
                                        code.Parameters
                                            .Where(static x => x.Documentation.DescriptionAvailable)
                                            .OrderBy(static x => x.Name)
                                            .Select(x => $"@param {x.Name} {x.Documentation.GetDescription(type => GetTypescriptTypeString(type, codeFile, inlineComposedTypeString: true), ReferenceTypePrefix, ReferenceTypeSuffix, RemoveInvalidDescriptionCharacters)}")
                                            .Union([returnRemark])
                                            .Union(GetThrownExceptionsRemarks(code)));
    }
    private static IEnumerable<string> GetThrownExceptionsRemarks(CodeMethod code)
    {
        if (code.Kind is not CodeMethodKind.RequestExecutor) yield break;
        foreach (var errorMapping in code.ErrorMappings)
        {
            var statusCode = errorMapping.Key.ToUpperInvariant() switch
            {
                "XXX" => "4XX or 5XX",
                _ => errorMapping.Key,
            };
            var errorTypeString = GetTypescriptTypeString(errorMapping.Value, code, false, inlineComposedTypeString: true);
            yield return $"@throws {{{errorTypeString}}} error when the service returns a {statusCode} status code";
        }
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
        var exportPrefix = code.Access is AccessModifier.Public ? "export" : string.Empty;
        var functionPrefix = isFunction ? $"{exportPrefix}{asyncPrefix.TrimEnd()} function " : " ";
        var parameters = string.Join(", ", code.Parameters.Order(parameterOrderComparer).Select(p => pConventions.GetParameterSignature(p, code)));
        var asyncReturnTypePrefix = code.IsAsync ? "Promise<" : string.Empty;
        var asyncReturnTypeSuffix = code.IsAsync ? ">" : string.Empty;
        var nullableSuffix = code.ReturnType.IsNullable && !isVoid ? " | undefined" : string.Empty;
        var shouldHaveTypeSuffix = !code.IsAccessor && !isConstructor && !string.IsNullOrEmpty(returnType);
        var returnTypeSuffix = shouldHaveTypeSuffix ? $" : {asyncReturnTypePrefix}{returnType}{nullableSuffix}{asyncReturnTypeSuffix}" : string.Empty;
        var openBracketSuffix = code.Parent is CodeClass || isFunction ? " {" : ";";
        writer.WriteLine($"{accessModifier}{functionPrefix}{staticPrefix}{methodName}{(isFunction ? string.Empty : asyncPrefix)}({parameters}){returnTypeSuffix}{openBracketSuffix}");
    }

    internal static void WriteMethodTypecheckIgnoreInternal(CodeMethod code, LanguageWriter writer)
    {
        writer.WriteLine("// @ts-ignore");
    }
}
