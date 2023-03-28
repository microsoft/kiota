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
        _codeUsingWriter.WriteConditionalInternalImports(codeElement, writer, parentNamespace);

        if (codeElement.Parent is CodeClass parentClass)
        {
            if (codeElement.Inherits != null)
                _codeUsingWriter.WriteDeferredImport(parentClass, codeElement.Inherits.Name, writer);
            foreach (var implement in codeElement.Implements)
                _codeUsingWriter.WriteDeferredImport(parentClass, implement.Name, writer);

        }


        var abcClass = !codeElement.Implements.Any() ? string.Empty : $"{codeElement.Implements.Select(static x => x.Name.ToFirstCharacterUpperCase()).Aggregate((x, y) => x + ", " + y)}";
        var derivation = codeElement.Inherits is CodeType inheritType &&
                        conventions.GetTypeString(inheritType, codeElement) is string inheritSymbol &&
                        !string.IsNullOrEmpty(inheritSymbol) ?
                            inheritSymbol :
                            abcClass;



        if (codeElement.Parent?.Parent is CodeClass)
        {
            writer.WriteLine("@dataclass");
        }
        writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}({derivation}):");
        writer.IncreaseIndent();
        if (codeElement.Parent is CodeClass parent)
            conventions.WriteShortDescription(parent.Documentation.Description, writer);
    }
}
