using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

public class CodeFile : CodeBlock<CodeFileDeclaration, CodeFileBlockEnd>
{

    public CodeFile(string name, params CodeElement[] children)
    {
        Name = name;
        AddElements(children);
    }

    private IEnumerable<T> AddElements<T>(params T[] elements) where T : CodeElement
    {
        if (elements == null || elements.Any(x => x == null))
            throw new ArgumentNullException(nameof(elements));
        if (!elements.Any())
            throw new ArgumentOutOfRangeException(nameof(elements));

        return AddRange(elements);
    }

    public IEnumerable<CodeClass> AddClasses(params CodeClass[] codeClasses) => AddElements(codeClasses);

    public IEnumerable<CodeInterface> AddInterfaces(params CodeInterface[] codeInterfaces) =>
        AddElements(codeInterfaces);

    public IEnumerable<CodeUsing> GetUsings()
    {
        return GetChildElements().SelectMany(x => x.GetChildElements())
            .Where(x => x is ProprietableBlockDeclaration)
            .Cast<ProprietableBlockDeclaration>()
            .SelectMany(x => x.Usings);
    }
}
public class CodeFileDeclaration : ProprietableBlockDeclaration
{
}

public class CodeFileBlockEnd : BlockEnd
{
}
