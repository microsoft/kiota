using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;
public class CodeNameSpaceWriter : BaseElementWriter<CodeNamespace, TypeScriptConventionService>
{
    public CodeNameSpaceWriter(TypeScriptConventionService conventionService) : base(conventionService) { }

    /// <summary>
    /// Writes export statements for classes and enums belonging to a namespace into a generated index.ts file. 
    /// The classes should be export in the order of inheritance so as to avoid circular dependency issues in javascript.
    /// </summary>
    /// <param name="codeElement">Code element is a code namespace</param>
    /// <param name="writer"></param>
    public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
    {
        writer.WriteLines(codeElement.Enums
                                    .Concat<CodeElement>(codeElement.Functions)
                                    .Concat(codeElement.Interfaces)
                                    .OrderBy(static x => x is CodeEnum ? 0 : 1)
                                    .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                                    .Select(static x => x.Name.ToFirstCharacterLowerCase())
                                    .Select(static x => $"export * from './{x}'"));
    }
}
