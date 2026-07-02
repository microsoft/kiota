using System;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;

public class CodePropertyWriter : BaseElementWriter<CodeProperty, GoConventionService>
{
    public CodePropertyWriter(GoConventionService conventionService) : base(conventionService) { }
    /// <summary>
    /// Only enforces the Go invariants. The field itself is written by
    /// <see cref="CodeClassDeclarationWriter"/>, which needs the whole struct body at once to
    /// column-align the fields the way gofmt does.
    /// </summary>
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        switch (codeElement.Kind)
        {
            case CodePropertyKind.RequestBuilder:
                throw new InvalidOperationException("RequestBuilders are as properties are not supported in Go and should be replaced by methods by the refiner.");
            case CodePropertyKind.ErrorMessageOverride:
                throw new InvalidOperationException("Error message overrides are implemented with methods in Go.");
        }
    }
    internal static void WriteField(CodeProperty codeElement, LanguageWriter writer, GoConventionService conventions)
    {
        if (codeElement.ExistsInExternalBaseType)
            return;
        var propertyName = codeElement.Access == AccessModifier.Public ? codeElement.Name.ToFirstCharacterUpperCase() : codeElement.Name.ToFirstCharacterLowerCase();
        var suffix = codeElement.Kind is CodePropertyKind.QueryParameter && codeElement.IsNameEscaped ?
            $" \"uriparametername:\\\"{codeElement.SerializationName.SanitizeDoubleQuote()}\\\"\"" :
            string.Empty;
        var returnType = codeElement.Parent is CodeElement parent ? conventions.GetTypeString(codeElement.Type, parent) : string.Empty;
        conventions.WriteShortDescription(codeElement, writer);
        conventions.WriteDeprecation(codeElement, writer);
        writer.WriteLine(string.IsNullOrEmpty(returnType) && string.IsNullOrEmpty(suffix) ? propertyName : $"{propertyName} {returnType}{suffix}");
    }
}
