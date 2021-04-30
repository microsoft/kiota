using System;

namespace Kiota.Builder.Writers.CSharp {
    public abstract class BaseCSharpElementWriter<T> : ICodeElementWriter<T> where T : CodeElement
    {
        public BaseCSharpElementWriter(CSharpConventionService conventionService)
        {
            conventions = conventionService ?? throw new ArgumentNullException(nameof(conventionService));
        }
        protected readonly CSharpConventionService conventions;
        public abstract void WriteCodeElement(T codeElement, LanguageWriter writer);
    }
}
