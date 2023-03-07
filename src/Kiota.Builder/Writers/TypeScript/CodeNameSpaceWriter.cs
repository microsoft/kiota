using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        var sb = new StringBuilder();
        foreach (var element in codeElement.Enums.Concat<CodeElement>(codeElement.Functions).Concat(codeElement.CodeInterfaces))
        {
            var name = element.Name.ToFirstCharacterLowerCase();
            sb.AppendLine($"export * from './{name}'");
        }
        writer.Write(sb.ToString());
    }
}
