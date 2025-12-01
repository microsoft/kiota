using System;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

public abstract class BaseElementWriter<TElement, TConventionsService> : ICodeElementWriter<TElement> where TElement : CodeElement where TConventionsService : ILanguageConventionService
{
    protected BaseElementWriter(TConventionsService conventionService)
    {
        conventions = conventionService ?? throw new ArgumentNullException(nameof(conventionService));
    }
    protected TConventionsService conventions
    {
        get; init;
    }
    public abstract void WriteCodeElement(TElement codeElement, LanguageWriter writer);
}
