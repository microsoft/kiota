using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Markdown;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, MarkdownConventionService>
{
    public CodeMethodWriter(MarkdownConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        if (codeElement == null) throw new ArgumentNullException(nameof(codeElement));
        if (codeElement.ReturnType == null) throw new InvalidOperationException($"{nameof(codeElement.ReturnType)} should not be null");
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (!(codeElement.Parent is CodeClass)) throw new InvalidOperationException("the parent of a method should be a class");

        var returnType = conventions.GetTypeString(codeElement.ReturnType, codeElement);
        var parentClass = codeElement.Parent as CodeClass;
        var inherits = parentClass.StartBlock.Inherits != null && !parentClass.IsErrorDefinition;
        var isVoid = conventions.VoidTypeName.Equals(returnType, StringComparison.OrdinalIgnoreCase);
 //       WriteMethodDocumentation(codeElement, writer);
        var codeMethod = codeElement as CodeMethod;
        // switch on kind of codeMethod
        switch(codeMethod.Kind) {
            case CodeMethodKind.RequestExecutor:
                WriteMethodPrototype(codeElement, writer, returnType, inherits, isVoid);
                break;
            default:
                break;
        }
    }

    private static readonly BaseCodeParameterOrderComparer parameterOrderComparer = new();
    private void WriteMethodPrototype(CodeMethod code, LanguageWriter writer, string returnType, bool inherits, bool isVoid)
    {
        var staticModifier = code.IsStatic ? "static " : string.Empty;
        var hideModifier = inherits && code.IsOfKind(CodeMethodKind.Serializer, CodeMethodKind.Deserializer, CodeMethodKind.Factory) ? "new " : string.Empty;
        var genericTypePrefix = isVoid ? string.Empty : "<";
        var genericTypeSuffix = code.IsAsync && !isVoid ? ">" : string.Empty;
        var isConstructor = code.IsOfKind(CodeMethodKind.Constructor, CodeMethodKind.ClientConstructor, CodeMethodKind.RawUrlConstructor);
        var asyncPrefix = code.IsAsync ? "async Task" + genericTypePrefix : string.Empty;
        var voidCorrectedTaskReturnType = code.IsAsync && isVoid ? string.Empty : returnType;
        if (code.ReturnType.IsArray && code.IsOfKind(CodeMethodKind.RequestExecutor))
            voidCorrectedTaskReturnType = $"IEnumerable<{voidCorrectedTaskReturnType.StripArraySuffix()}>";
        // TODO: Task type should be moved into the refiner
        var completeReturnType = isConstructor ?
            string.Empty :
            $" {voidCorrectedTaskReturnType}{genericTypeSuffix} ";
        var baseSuffix = string.Empty;
        if (isConstructor && inherits)
            baseSuffix = " : base()";
        var parameters = string.Join(", ", code.Parameters.OrderBy(x => x, parameterOrderComparer).Select(p => conventions.GetParameterSignature(p, code)).ToList());
        var methodName = isConstructor ? code.Parent.Name.ToFirstCharacterUpperCase() : code.Name.ToFirstCharacterUpperCase();
        writer.WriteLine($"| {methodName} | {parameters} | {completeReturnType} | ");
    }
    
}
