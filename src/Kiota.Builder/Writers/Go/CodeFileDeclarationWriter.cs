﻿using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;

public class CodeFileDeclarationWriter : BaseElementWriter<CodeFileDeclaration, GoConventionService>
{
    public CodeFileDeclarationWriter(GoConventionService conventionService) : base(conventionService) { }

    public override void WriteCodeElement(CodeFileDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is CodeFile cs && cs.Parent is CodeNamespace ns)
        {
            // always add a comment to the top of the file to indicate it's generated
            conventions.WriteGeneratorComment(writer);
            writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)}");
            var importSegments = cs
                                .AllUsingsFromChildElements
                                .Where(x => x.Declaration != null && !x.Declaration.IsExternal && !x.Name.Equals(ns.Name, StringComparison.OrdinalIgnoreCase) &&
                                        x.Declaration.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>() != ns)
                                .Select(static x => x.GetInternalNamespaceImport())
                                .Select(static x => new Tuple<string, string>(x.GetNamespaceImportSymbol(), x))
                                .Distinct()
                                .Union(cs
                                    .AllUsingsFromChildElements
                                    .Union(codeElement.Parent is CodeClass currentClass ? currentClass.InnerClasses.SelectMany(static x => x.Usings) : Enumerable.Empty<CodeUsing>())
                                    .Where(static x => x.Declaration != null && x.Declaration.IsExternal)
                                    .Select(static x => new Tuple<string, string>(x.Name.StartsWith('*') ? x.Name[1..] : x.Declaration!.Name.GetNamespaceImportSymbol(), x.Declaration!.Name))
                                    .Distinct())
                                .OrderBy(static x => x.Item2) // Item1: import alias, Item2: import path
                                .ToList();
            if (importSegments.Count != 0)
            {
                writer.WriteLines(string.Empty, "import (");
                writer.IncreaseIndent();
                importSegments.ForEach(x => writer.WriteLine(string.IsNullOrEmpty(x.Item1) ? $"\"{x.Item2}\"" : $"{x.Item1} \"{x.Item2}\""));
                writer.DecreaseIndent();
                writer.WriteLines(")", string.Empty);
            }
        }
    }

}
