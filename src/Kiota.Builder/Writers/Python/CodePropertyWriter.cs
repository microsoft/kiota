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
        if (codeElement.ExistsInExternalBaseType) return;
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
                conventions.WriteLongDescription(codeElement.Documentation, writer);
                _codeUsingWriter.WriteDeferredImport(parentClass, codeElement.Type.Name, writer);
                conventions.AddRequestBuilderBody(parentClass, returnType, writer);
                writer.CloseBlock(string.Empty);
                break;
            case CodePropertyKind.QueryParameters:
                returnType = $"{codeElement.Parent?.Parent?.Name.ToFirstCharacterUpperCase()}.{codeElement.Type.Name.ToFirstCharacterUpperCase()}";
                goto case CodePropertyKind.Headers;
            case CodePropertyKind.Headers:
            case CodePropertyKind.Options:
            case CodePropertyKind.QueryParameter:
                conventions.WriteInLineDescription(codeElement.Documentation.Description, writer);
                writer.WriteLine($"{conventions.GetAccessModifier(codeElement.Access)}{codeElement.NamePrefix}{codeElement.Name.ToSnakeCase()}: {(codeElement.Type.IsNullable ? "Optional[" : string.Empty)}{returnType}{(codeElement.Type.IsNullable ? "]" : string.Empty)} = None");
                writer.WriteLine();
                break;
            case CodePropertyKind.ErrorMessageOverride when parentClass.IsErrorDefinition:
                writer.WriteLine("@property");
                writer.StartBlock($"def {codeElement.Name}(self) -> {codeElement.Type.Name}:");
                conventions.WriteLongDescription(codeElement.Documentation, writer);
                if (parentClass.GetPrimaryMessageCodePath(static x => x.Name.ToFirstCharacterLowerCase(),
                        static x => x.Name.ToSnakeCase()) is string primaryMessageCodePath &&
                    !string.IsNullOrEmpty(primaryMessageCodePath))
                {
                    var pathWithoutMessage = primaryMessageCodePath.TrimEnd(".message".ToCharArray());
                    writer.StartBlock($"if self.{pathWithoutMessage} is not None:");
                    writer.WriteLine(
                        $"return '' if self.{primaryMessageCodePath} is None else self.{primaryMessageCodePath}");
                    writer.DecreaseIndent();
                    writer.WriteLine("return ''");
                }
                else
                    writer.WriteLine("return super().message");
                writer.DecreaseIndent();
                break;
        }
    }
}
