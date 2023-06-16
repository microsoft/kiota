using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

public class CodeFile : CodeBlock<CodeFileDeclaration, CodeFileBlockEnd>
{
    public IEnumerable<T> AddElements<T>(params T[] elements) where T : CodeElement
    {
        if (elements == null || elements.Any(static x => x == null))
            throw new ArgumentNullException(nameof(elements));
        if (!elements.Any())
            throw new ArgumentOutOfRangeException(nameof(elements));

        return AddRange(elements);
    }

    public IEnumerable<CodeUsing> AllUsingsFromChildElements => GetChildElements()
            .SelectMany(static x => x.GetChildElements())
            .OfType<ProprietableBlockDeclaration>()
            .SelectMany(static x => x.Usings);
}
public class CodeFileDeclaration : ProprietableBlockDeclaration
{
}

public class CodeFileBlockEnd : BlockEnd
{
}
