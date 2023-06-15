using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, PythonConventionService>
{
    private readonly CodeUsingWriter _codeUsingWriter;
    public CodePropertyWriter(PythonConventionService conventionService, string clientNamespaceName) : base(conventionService)
    {
        _codeUsingWriter = new(clientNamespaceName);
    }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var returnType = conventions.GetTypeString(codeElement.Type, codeElement, true, writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException("The parent of a property should be a class");
        /* Only write specific properties as class attributes
        * The rest will be implemented as instance attributes, to avoid mutable properties
        * from being modified across instances. 
        */
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                writer.WriteLine("@property");
                writer.WriteLine($"def {codeElement.Name.ToSnakeCase()}(self) -> {returnType}:");
                writer.IncreaseIndent();
                conventions.WriteShortDescription(codeElement.Documentation.Description, writer);
                _codeUsingWriter.WriteDeferredImport(parentClass, codeElement.Type.Name, writer);
                conventions.AddRequestBuilderBody(parentClass, returnType, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodePropertyKind.QueryParameters:
            case CodePropertyKind.Headers:
            case CodePropertyKind.Options:
            case CodePropertyKind.QueryParameter:
                conventions.WriteInLineDescription(codeElement.Documentation.Description, writer);
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)}{codeElement.NamePrefix}{codeElement.Name.ToSnakeCase()}: {(codeElement.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(codeElement.Type.IsNullable ? "]" : string.Empty)} = None");
                writer.WriteLine();
                break;
        }
    }
}
