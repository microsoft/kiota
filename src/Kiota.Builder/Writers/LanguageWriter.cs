using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Java;
using Kiota.Builder.Writers.TypeScript;

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
            return indentString.Substring(0, Math.Max(0, currentIndent));
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
        public void Write<T>(T code) where T : CodeElement
        {
            _ = Writers.TryGetValue(code.GetType(), out var elementWriter);
            switch(code) {
                case CodeProperty p: // we have to do this triage because dotnet is limited in terms of covariance
                    ((ICodeElementWriter<CodeProperty>) elementWriter).WriteCodeElement(p, this);
                    break;
                case CodeIndexer i:
                    ((ICodeElementWriter<CodeIndexer>) elementWriter).WriteCodeElement(i, this);
                    break;
                case CodeClass.Declaration d:
                    ((ICodeElementWriter<CodeClass.Declaration>) elementWriter).WriteCodeElement(d, this);
                    break;
                case CodeClass.End i:
                    ((ICodeElementWriter<CodeClass.End>) elementWriter).WriteCodeElement(i, this);
                    break;
                case CodeEnum e:
                    ((ICodeElementWriter<CodeEnum>) elementWriter).WriteCodeElement(e, this);
                    break;
                case CodeMethod m:
                    ((ICodeElementWriter<CodeMethod>) elementWriter).WriteCodeElement(m, this);
                    break;
                case CodeType t:
                    ((ICodeElementWriter<CodeType>) elementWriter).WriteCodeElement(t, this);
                    break;
                case CodeNamespace.BlockDeclaration:
                case CodeNamespace.BlockEnd:
                case CodeNamespace:
                case CodeClass:
                    break;
                default:
                    throw new InvalidOperationException($"Dispatcher missing for type {code.GetType()}");
            }
        }
        protected void AddCodeElementWriter<T>(ICodeElementWriter<T> writer) where T: CodeElement {
            Writers.Add(typeof(T), writer);
        }
        private readonly Dictionary<Type, object> Writers = new(); // we have to type as object because dotnet doesn't have type capture i.e eq for `? extends CodeElement`
        public static LanguageWriter GetLanguageWriter(GenerationLanguage language, string outputPath, string clientNamespaceName, bool usesBackingStore = false) {
            return language switch
            {
                GenerationLanguage.CSharp => new CSharpWriter(outputPath, clientNamespaceName, usesBackingStore),
                GenerationLanguage.Java => new JavaWriter(outputPath, clientNamespaceName),
                GenerationLanguage.TypeScript => new TypeScriptWriter(outputPath, clientNamespaceName, usesBackingStore),
                _ => throw new InvalidEnumArgumentException($"{language} language currently not supported."),
            };
        }
    }
}
