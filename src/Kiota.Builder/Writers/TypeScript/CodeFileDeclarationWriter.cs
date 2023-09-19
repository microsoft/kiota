using System;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeFileDeclarationWriter : BaseElementWriter<CodeFileDeclaration, TypeScriptConventionService>
{
    public CodeFileDeclarationWriter(TypeScriptConventionService conventionService) : base(conventionService) { }

    public override void WriteCodeElement(CodeFileDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is CodeFile cf && cf.Parent is CodeNamespace ns)
        {
            CodeUsingWriter codeUsingWriter = new(ns.Name);
            var usings = cf.GetChildElements().SelectMany(static x =>
                {
                    var startBlockUsings = x switch
                    {
                        CodeFunction f => f.StartBlock.Usings,
                        CodeInterface ci => ci.Usings,
                        CodeClass cc => cc.Usings,
                        _ => Enumerable.Empty<CodeUsing>()
                    };
                    return startBlockUsings;
                }
            );
            codeUsingWriter.WriteCodeElement(usings, ns, writer);
        }
    }

}
