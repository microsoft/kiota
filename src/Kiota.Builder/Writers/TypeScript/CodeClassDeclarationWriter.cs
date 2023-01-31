﻿using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, TypeScriptConventionService>
{
    private readonly CodeUsingWriter _codeUsingWriter;
    public CodeClassDeclarationWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService)
    {
        _codeUsingWriter = new(clientNamespaceName);
    }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        _codeUsingWriter.WriteCodeElement(codeElement.Usings, parentNamespace, writer);

        var inheritSymbol = codeElement.Inherits is null ? string.Empty : conventions.GetTypeString(codeElement.Inherits, codeElement);
        var derivation = (string.IsNullOrEmpty(inheritSymbol) ? string.Empty : $" extends {inheritSymbol}") +
                        (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}");
        if (codeElement.Parent is CodeClass parentClass)
            conventions.WriteLongDescription(parentClass.Documentation, writer);
        writer.WriteLine($"export class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} {{");
        writer.IncreaseIndent();
    }

}
