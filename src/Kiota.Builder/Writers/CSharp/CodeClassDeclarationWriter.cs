﻿using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.CSharp;
public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, CSharpConventionService>
{
    public static string AutoGenerationHeader => "// <auto-generated/>";
    public static string GeneratedCodeAttribute { get; } = $"[global::System.CodeDom.Compiler.GeneratedCode(\"Kiota\", \"{Kiota.Generated.KiotaVersion.CurrentMajor()}\")]";
    public CodeClassDeclarationWriter(CSharpConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeClass parentClass) throw new InvalidOperationException($"The provided code element {codeElement.Name} doesn't have a parent of type {nameof(CodeClass)}");

        if (codeElement.Parent?.Parent is CodeNamespace)
        {
            writer.WriteLine(AutoGenerationHeader);
            if (conventions.UseCSharp13)
            {
                writer.WriteLine(CSharpConventionService.NullableEnableDirective);
            }
            conventions.WritePragmaDisable(writer, CSharpConventionService.CS0618);
            codeElement.Usings
                    .Where(x => (x.Declaration?.IsExternal ?? true) || !x.Declaration.Name.Equals(codeElement.Name, StringComparison.OrdinalIgnoreCase)) // needed for circular requests patterns like message folder
                    .Select(static x => x.Declaration?.IsExternal ?? false ?
                                    $"using {x.Declaration.Name.NormalizeNameSpaceName(".")};" :
                                    $"using {x.Name.NormalizeNameSpaceName(".")};")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(static x => x, StringComparer.Ordinal)
                    .ToList()
                    .ForEach(x => writer.WriteLine(x));
            writer.WriteLine($"namespace {codeElement.Parent.Parent.Name}" + (conventions.UseCSharp13 ? ";" : string.Empty));
            if (!conventions.UseCSharp13)
            {
                writer.StartBlock();
            }
        }

        var derivedTypes = (codeElement.Inherits is null ? Enumerable.Empty<string?>() : new string?[] { conventions.GetTypeString(codeElement.Inherits, parentClass) })
                                        .Union(codeElement.Implements.Select(static x => x.Name))
                                        .OfType<string>()
                                        .ToArray();
        var derivation = derivedTypes.Length != 0 ? ": " + derivedTypes.Aggregate(static (x, y) => $"{x}, {y}") : string.Empty;
        bool hasDescription = conventions.WriteLongDescription(parentClass, writer);
        conventions.WriteDeprecationAttribute(parentClass, writer);
        writer.WriteLine(GeneratedCodeAttribute);
        if (!hasDescription) conventions.WritePragmaDisable(writer, CSharpConventionService.CS1591);
        writer.WriteLine($"{conventions.GetAccessModifier(parentClass.Access)} partial class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}");
        if (!hasDescription) conventions.WritePragmaRestore(writer, CSharpConventionService.CS1591);
        writer.StartBlock();
    }
}
