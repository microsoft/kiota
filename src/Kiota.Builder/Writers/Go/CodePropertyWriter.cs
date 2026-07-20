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
        // This writer emits nothing: properties still reach it because they remain CodeProperty DOM
        // items — the refinement stage does not replace them with anything else. Ideally a new
        // "property group" DOM item, produced by a refiner transformation and rendered by its own
        // dedicated writer, would own the struct body, and this property writer could be removed
        // entirely. That is a bigger refactoring, so for now the switch below only enforces the
        // invariants the Go refiner is expected to have upheld.
        switch (codeElement.Kind)
        {
            // The Go refiner replaces request-builder properties with methods; one surviving to the
            // write stage is a refiner bug.
            case CodePropertyKind.RequestBuilder:
                throw new InvalidOperationException("RequestBuilders are as properties are not supported in Go and should be replaced by methods by the refiner.");
            // Likewise implemented as methods in Go, so this kind must not reach the writer either.
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
