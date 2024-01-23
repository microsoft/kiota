using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.CodeDOM;

public class CodeFile : CodeBlock<CodeFileDeclaration, CodeFileBlockEnd>
{
    public IEnumerable<CodeInterface> Interfaces => InnerChildElements.Values.OfType<CodeInterface>().OrderBy(static x => x.Name, StringComparer.Ordinal);
    public IEnumerable<CodeClass> Classes => InnerChildElements.Values.OfType<CodeClass>().OrderBy(static x => x.Name, StringComparer.Ordinal);
    public IEnumerable<CodeEnum> Enums => InnerChildElements.Values.OfType<CodeEnum>().OrderBy(static x => x.Name, StringComparer.Ordinal);
    public IEnumerable<CodeConstant> Constants => InnerChildElements.Values.OfType<CodeConstant>().OrderBy(static x => x.Name, StringComparer.Ordinal);

    public IEnumerable<T> AddElements<T>(params T[] elements) where T : CodeElement
    {
        if (elements == null || elements.Any(static x => x == null))
            throw new ArgumentNullException(nameof(elements));
        if (elements.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(elements));

        return AddRange(elements);
    }

    public IEnumerable<CodeUsing> AllUsingsFromChildElements => GetChildElements(true)
        .SelectMany(static x => x.GetChildElements(false))
        .OfType<ProprietableBlockDeclaration>()
        .SelectMany(static x => x.Usings);
}
public class CodeFileDeclaration : ProprietableBlockDeclaration
{
}

public class CodeFileBlockEnd : BlockEnd
{
}
