﻿using System;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.TypeScript;

public class CodeFileDeclarationWriter : BaseElementWriter<CodeFileDeclaration, TypeScriptConventionService>
{
    private readonly CodeUsingWriter _codeUsingWriter;
    public CodeFileDeclarationWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService)
    {
        _codeUsingWriter = new(clientNamespaceName);
    }

    public override void WriteCodeElement(CodeFileDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is CodeFile cf && cf.Parent is CodeNamespace ns)
        {
            var usings = cf.GetChildElements().SelectMany(static x =>
                {
                    return x switch
                    {
                        CodeFunction f => f.StartBlock.Usings,
                        CodeInterface ci => ci.Usings,
                        CodeClass cc => cc.Usings,
                        _ => Enumerable.Empty<CodeUsing>()
                    };
                }
            );
            conventions.WriteAutoGeneratedStart(writer);

            // remove duplicate using, keep a single using for each internal type in the same namespace
            var enumeratedUsing = usings.ToArray();
            var filteredUsing = enumeratedUsing.Where(static x => x.IsExternal)
                .Union(enumeratedUsing.ToArray()
                    .Where(static x => x is { IsExternal: false, Declaration.TypeDefinition: not null })
                    .GroupBy(static x =>
                        $"{x.Declaration!.TypeDefinition!.GetImmediateParentOfType<CodeNamespace>().Name}.{x.Declaration?.Name.ToLowerInvariant()}")
                    .Select(static x => x.OrderBy(static x => x.Parent?.Name).First()));

            var array = filteredUsing.ToArray();
            if (!array.Any())
            {
                throw new InvalidOperationException($"File missing imports {cf.Name}, name space: {ns.Name}");
            }

            _codeUsingWriter.WriteCodeElement(filteredUsing, ns, writer);
        }
    }

}
