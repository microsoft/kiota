﻿using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;

public class CodeInterfaceDeclarationWriter : CodeProprietableBlockDeclarationWriter<InterfaceDeclaration>
{
    public CodeInterfaceDeclarationWriter(GoConventionService conventionService) : base(conventionService) { }
    protected override void WriteTypeDeclaration(InterfaceDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeInterface inter) throw new InvalidOperationException("Expected the parent to be an interface");
        var interName = codeElement.Name.ToFirstCharacterUpperCase();
        conventions.WriteShortDescription(inter, writer, $"A {interName}");
        if (codeElement.Parent is CodeInterface currentInterface && currentInterface.OriginalClass is not null)
            conventions.WriteDeprecation(currentInterface.OriginalClass, writer);
        conventions.WriteLinkDescription(inter.Documentation, writer);
        writer.WriteLine($"type {interName} interface {{");
        writer.IncreaseIndent();
        foreach (var implement in codeElement.Implements)
        {
            var parentTypeName = conventions.GetTypeString(implement, inter, true, false);
            writer.WriteLine(parentTypeName);
        }
    }
}
