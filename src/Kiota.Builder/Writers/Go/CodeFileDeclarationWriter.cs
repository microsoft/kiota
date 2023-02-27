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
        if (codeElement.Parent is CodeFile cs && codeElement.Parent?.Parent is CodeNamespace ns)
        {
            writer.WriteLine($"package {ns.Name.GetLastNamespaceSegment().Replace("-", string.Empty)}");
            var importSegments = cs
                                .GetUsings()
                                .Where(x => x.Declaration != null && !x.Declaration.IsExternal && !x.Name.Equals(ns.Name, StringComparison.OrdinalIgnoreCase) &&
                                        x.Declaration.TypeDefinition?.GetImmediateParentOfType<CodeNamespace>() != ns)
                                .Select(static x => x.GetInternalNamespaceImport())
                                .Select(static x => new Tuple<string, string>(x.GetNamespaceImportSymbol(), x))
                                .Distinct()
                                .Union(cs
                                    .GetUsings()
                                    .Union(codeElement.Parent is CodeClass currentClass ? currentClass.InnerClasses.SelectMany(static x => x.Usings) : Enumerable.Empty<CodeUsing>())
                                    .Where(static x => x.Declaration != null && x.Declaration.IsExternal)
                                    .Select(static x => new Tuple<string, string>(x.Name.StartsWith("*") ? x.Name[1..] : x.Declaration!.Name.GetNamespaceImportSymbol(), x.Declaration!.Name))
                                    .Distinct())
                                .OrderBy(static x => x.Item2.Count(static y => y == '/'))
                                .ThenBy(static x => x)
                                .ToList();
            if (importSegments.Any())
            {
                writer.WriteLines(string.Empty, "import (");
                writer.IncreaseIndent();
                importSegments.ForEach(x => writer.WriteLine(x.Item1.Equals(x.Item2) ? $"\"{x.Item2}\"" : $"{x.Item1} \"{x.Item2}\""));
                writer.DecreaseIndent();
                writer.WriteLines(")", string.Empty);
            }
        }
    }

}
