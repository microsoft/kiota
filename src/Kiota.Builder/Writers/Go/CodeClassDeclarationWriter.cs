using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;

public class CodeClassDeclarationWriter : CodeProprietableBlockDeclarationWriter<ClassDeclaration>
{
    public CodeClassDeclarationWriter(GoConventionService conventionService) : base(conventionService) { }
    protected override void WriteTypeDeclaration(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var className = codeElement.Name.ToFirstCharacterUpperCase();
        if (codeElement.Parent is not CodeClass currentClass) throw new InvalidOperationException("The parent of a class declaration should be a class");
        conventions.WriteShortDescription(currentClass, writer, $"{className} ");
        conventions.WriteDeprecation(currentClass, writer);
        conventions.WriteLinkDescription(currentClass.Documentation, writer);
        writer.StartBlock($"type {className} struct {{");
        // the whole struct body is buffered so the fields can be column-aligned like gofmt, which
        // pads sibling fields against each other and therefore needs to see all of them at once
        var body = writer.CaptureLines(() =>
        {
            if (codeElement.Inherits?.AllTypes?.Any() ?? false)
            {
                var parentTypeName = conventions.GetTypeString(codeElement.Inherits.AllTypes.First(), currentClass, true, false);
                writer.WriteLine(parentTypeName);
            }
            foreach (var property in currentClass.Properties
                                                .Where(static x => !x.ExistsInBaseType)
                                                .OrderBy(static x => x.Name, StringComparer.InvariantCultureIgnoreCase))
                CodePropertyWriter.WriteField(property, writer, conventions);
        });
        foreach (var line in GoFieldFormatter.AlignFieldBlock(body))
            writer.WriteLine(line, includeIndent: false);
    }
}
