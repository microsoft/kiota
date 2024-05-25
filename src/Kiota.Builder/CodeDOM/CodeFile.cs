using System;
using System.Collections.Generic;

namespace Kiota.Builder.CodeDOM;

public class CodeFile : CodeBlock<CodeFileDeclaration, CodeFileBlockEnd>
{
    public IEnumerable<CodeInterface> Interfaces
    {
        get
        {
            var interfaces = new List<CodeInterface>();
            foreach (var element in InnerChildElements.Values)
                if (element is CodeInterface codeInterface) interfaces.Add(codeInterface);
            interfaces.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            return interfaces;
        }
    }

    public IEnumerable<CodeClass> Classes
    {
        get
        {
            var classes = new List<CodeClass>();
            foreach (var element in InnerChildElements.Values)
                if (element is CodeClass codeClass) classes.Add(codeClass);
            classes.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            return classes;
        }
    }

    public IEnumerable<CodeEnum> Enums
    {
        get
        {
            var enums = new List<CodeEnum>();
            foreach (var element in InnerChildElements.Values)
                if (element is CodeEnum codeEnum) enums.Add(codeEnum);
            enums.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            return enums;
        }
    }

    public IEnumerable<CodeConstant> Constants
    {
        get
        {
            var constants = new List<CodeConstant>();
            foreach (var element in InnerChildElements.Values)
                if (element is CodeConstant codeConstant) constants.Add(codeConstant);
            constants.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            return constants;
        }
    }

    public IEnumerable<T> AddElements<T>(params T[] elements) where T : CodeElement
    {
        if (elements == null || Array.Exists(elements, x => x == null))
            throw new ArgumentNullException(nameof(elements));
        if (elements.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(elements));

        return AddRange(elements);
    }

    public IEnumerable<CodeUsing> AllUsingsFromChildElements
    {
        get
        {
            var allUsings = new List<CodeUsing>();
            foreach (var childElement in GetChildElements(true))
            {
                foreach (var subChildElement in childElement.GetChildElements(false))
                {
                    if (subChildElement is ProprietableBlockDeclaration blockDeclaration)
                    {
                        allUsings.AddRange(blockDeclaration.Usings);
                    }
                }
            }
            return allUsings;
        }
    }
}
public class CodeFileDeclaration : ProprietableBlockDeclaration
{
}

public class CodeFileBlockEnd : BlockEnd
{
}
