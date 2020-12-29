using System;
using System.IO;

namespace kiota.core
{
 
    public abstract class LanguageWriter
    {
        private TextWriter writer;
        const int indentSize = 4;
        private string indentString = "                                                                                             ";
        private int currentIndent = 0;

        /// <summary>
        /// This method must be called before you can use the writer method
        /// </summary>
        /// <param name="writer"></param>
        /// <remarks>Passing this to the constructor is problematic because for writing to files, an instance of this
        /// class is needed to get the file suffix to be able to create the filestream to create the writer.
        /// By making this a separate step, we can instantiate the LanguageWriter, then get the suffix, then create the writer.</remarks>
        public void SetTextWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public abstract string GetFileSuffix();

        public void IncreaseIndent()
        {
            currentIndent += indentSize;
        }

        public void DecreaseIndent()
        {
            currentIndent -= indentSize;
        }

        public string GetIndent()
        {
            return indentString.Substring(0, currentIndent);
        }

        protected void WriteLine(string line, bool includeIndent = true)
        {
            writer.WriteLine(includeIndent ? GetIndent() + line : line);
        }

        protected void Write(string text, bool includeIndent = true)
        {
            writer.Write(includeIndent ? GetIndent() + text : text);
        }
        /// <summary>
        /// Dispatch call to Write the code element to the proper derivative write method
        /// </summary>
        /// <param name="code"></param>
        public void Write(CodeElement code)
        {
            switch (code)
            {
                case CodeNamespace.Declaration c: WriteNamespaceDeclaration(c); break;
                case CodeNamespace.End c: WriteNamespaceEnd(c); break;
                case CodeClass.Declaration c: WriteCodeClassDeclaration(c); break;
                case CodeClass.End c: WriteCodeClassEnd(c); break;
                case CodeProperty c: WriteProperty(c); break;
                case CodeIndexer c: WriteIndexer(c); break;
                case CodeMethod c: WriteMethod(c); break;
                case CodeType c: WriteType(c); break;
                case CodeNamespace: break;
                case CodeClass: break;
                default:
                    throw new ArgumentException($"Dispatcher missing for type {code.GetType()}");
            }

        }


        public virtual void WriteProperty(CodeProperty code)
        {
        }

        public virtual void WriteIndexer(CodeIndexer code)
        {
        }
        public virtual void WriteMethod(CodeMethod code)
        {
        }

        public virtual void WriteType(CodeType code)
        {
        }

        public virtual void WriteNamespaceEnd(CodeNamespace.End code)
        {
        }

        public virtual void WriteNamespaceDeclaration(CodeNamespace.Declaration code)
        {
        }

        public virtual void WriteCodeClassDeclaration(CodeClass.Declaration code)
        {
        }

        public virtual void WriteCodeClassEnd(CodeClass.End code)
        {
        }
    }
}
