using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

// Generic base class for writing code elements
public class GenericWriter<TElement>(ILanguageConventionService conventionService)
    : BaseElementWriter<TElement, ILanguageConventionService>(conventionService)
    where TElement : CodeElement
{
    public override void WriteCodeElement(TElement codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
    }
}

// Specific writers inheriting from the generic class
public class GenericCodePropertyWriter(ILanguageConventionService conventionService)
    : GenericWriter<CodeProperty>(conventionService)
{
}

public class GenericCodeMethodWriter(ILanguageConventionService conventionService)
    : GenericWriter<CodeMethod>(conventionService)
{
}

public class GenericCodeElementWriter(ILanguageConventionService conventionService)
    : GenericWriter<CodeElement>(conventionService)
{
}
