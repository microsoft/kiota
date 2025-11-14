using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python;

public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, PythonConventionService>
{
    private readonly CodeUsingWriter _codeUsingWriter;
    public CodeClassDeclarationWriter(PythonConventionService conventionService, string clientNamespaceName) : base(conventionService)
    {
        _codeUsingWriter = new(clientNamespaceName);
    }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        _codeUsingWriter.WriteExternalImports(codeElement, writer); // external imports before internal imports
        if (codeElement.Parent?.Parent is not CodeClass) //Internal imports for inner classes will be written locally
        {
            _codeUsingWriter.WriteConditionalInternalImports(codeElement, writer, parentNamespace);
        }

        WriteParentClassImportsAndDecorators(codeElement, writer);

        var derivation = GetDerivation(codeElement);
        writer.WriteLine($"class {codeElement.Name}({derivation}):");
        writer.IncreaseIndent();
        WriteInnerClassImportsAndDescriptions(codeElement, writer, parentNamespace);
    }

    private void WriteParentClassImportsAndDecorators(ClassDeclaration codeElement, LanguageWriter writer)
    {
        if (codeElement.Parent is CodeClass parentClass)
        {
            if (codeElement.Inherits != null)
                _codeUsingWriter.WriteDeferredImport(parentClass, codeElement.Inherits.Name, writer);
            foreach (var implement in codeElement.Implements)
                _codeUsingWriter.WriteDeferredImport(parentClass, implement.Name, writer);
            if (parentClass.IsOfKind(CodeClassKind.Model) || parentClass.Parent is CodeClass)
            {
                writer.WriteLine("@dataclass");
            }
        }
    }

    private string GetDerivation(ClassDeclaration codeElement)
    {
        var abcClass = !codeElement.Implements.Any() ? string.Empty : $"{codeElement.Implements.Select(static x => x.Name).Aggregate((x, y) => x + ", " + y)}";
        var baseClass = codeElement.Inherits is CodeType inheritType &&
                        conventions.GetTypeString(inheritType, codeElement) is string inheritSymbol &&
                        !string.IsNullOrEmpty(inheritSymbol) ?
                            inheritSymbol :
                            string.Empty;
        if (string.IsNullOrEmpty(baseClass))
        {
            return abcClass;
        }
        else if (string.IsNullOrEmpty(abcClass))
        {
            return baseClass;
        }
        else
        {
            return $"{baseClass}, {abcClass}";
        }
    }

    private void WriteInnerClassImportsAndDescriptions(ClassDeclaration codeElement, LanguageWriter writer, CodeNamespace parentNamespace)
    {
        if (codeElement.Parent is CodeClass parent)
        {
            if (parent.Parent is CodeClass) // write imports for inner classes
            {
                _codeUsingWriter.WriteExternalImports(codeElement, writer);
                _codeUsingWriter.WriteConditionalInternalImports(codeElement, writer, parentNamespace);
            }
            conventions.WriteLongDescription(parent, writer);
            conventions.WriteDeprecationWarning(parent, writer);
        }
    }
}
