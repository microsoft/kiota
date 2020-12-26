using System.Collections.Generic;

namespace kiota.core
{
    /// <summary>
    /// Abstract element of some piece of source code to be generated
    /// </summary>
    public abstract class CodeElement
    {
        public abstract IList<CodeElement> GetChildElements();

        public abstract string Name
        {
            get; set;
        }

        public void Render(LanguageWriter writer)
        {
            writer.Write(this);
        }
    }
}
