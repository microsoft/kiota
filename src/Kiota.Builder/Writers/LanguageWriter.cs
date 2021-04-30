using System;
using System.Collections.Generic;
using System.IO;

namespace Kiota.Builder.Writers
{
 
    public abstract class LanguageWriter
    {
        private TextWriter writer;
        private const int indentSize = 4;
        private static readonly string indentString = "                                                                                             ";
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
        public IPathSegmenter PathSegmenter { get; protected set; }

        private readonly Stack<int> factorStack = new Stack<int>();
        public void IncreaseIndent(int factor = 1)
        {
            factorStack.Push(factor);
            currentIndent += indentSize * factor;
        }

        public void DecreaseIndent()
        {
            var popped = factorStack.TryPop(out var factor);
            currentIndent -= indentSize * (popped ? factor : 1);
        }

        public string GetIndent()
        {
            return indentString.Substring(0, currentIndent);
        }
        public static string NewLine { get => Environment.NewLine;}
        /// <summary>
        /// Adds an empty line
        /// </summary>
        internal void WriteLine() => WriteLine(string.Empty, false);
        internal void WriteLine(string line, bool includeIndent = true)
        {
            writer.WriteLine(includeIndent ? GetIndent() + line : line);
        }
        internal void WriteLines(params string[] lines) {
            foreach(var line in lines) {
                WriteLine(line, true);
            }
        }

        internal void Write(string text, bool includeIndent = true)
        {
            writer.Write(includeIndent ? GetIndent() + text : text);
        }
        /// <summary>
        /// Dispatch call to Write the code element to the proper derivative write method
        /// </summary>
        /// <param name="code"></param>
        public void Write(CodeElement code)
        {
            if(Writers.TryGetValue(code.GetType(), out var writer))
                writer.WriteCodeElement(code, this);
            else
                throw new InvalidOperationException($"Dispatcher missing for type {code.GetType()}");
        }
        public Dictionary<Type, ICodeElementWriter<CodeElement>> Writers { get; protected set; }
    }
}
