using System;

namespace Kiota.Builder.Writers {
    public abstract class BaseElementWriter<T, U> : ICodeElementWriter<T> where T : CodeElement where U : ILanguageConventionService
    {
        protected BaseElementWriter(U conventionService)
        {
            conventions = conventionService ?? throw new ArgumentNullException(nameof(conventionService));
        }
        protected readonly U conventions;
        public abstract void WriteCodeElement(T codeElement, LanguageWriter writer);
    }
}
