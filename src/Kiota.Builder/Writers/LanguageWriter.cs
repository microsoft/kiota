﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.Java;
using Kiota.Builder.Writers.Ruby;
using Kiota.Builder.Writers.Shell;
using Kiota.Builder.Writers.TypeScript;
using Kiota.Builder.Writers.Php;
using Kiota.Builder.Writers.Swift;

namespace Kiota.Builder.Writers
{
 
    public abstract class LanguageWriter
    {
        private TextWriter writer;
        private const int IndentSize = 4;
        private static readonly string indentString = Enumerable.Repeat(" ", 1000).Aggregate((x, y) => x + y);
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

        private readonly Stack<int> factorStack = new();
        public void IncreaseIndent(int factor = 1)
        {
            factorStack.Push(factor);
            currentIndent += IndentSize * factor;
        }

        public void DecreaseIndent()
        {
            var popped = factorStack.TryPop(out var factor);
            currentIndent -= IndentSize * (popped ? factor : 1);
        }

        public string GetIndent()
        {
            return indentString[..Math.Max(0, currentIndent)];
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
        internal void CloseBlock(string symbol = "}", bool decreaseIndent = true)
        {
            if (decreaseIndent)
                DecreaseIndent();
            WriteLine(symbol);
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
            if(Writers.TryGetValue(code.GetType(), out var elementWriter))
                switch(code) {
                    case CodeProperty p when !p.ExistsInBaseType: // to avoid duplicating props on inheritance structure
                        ((ICodeElementWriter<CodeProperty>) elementWriter).WriteCodeElement(p, this);
                        break;
                    case CodeIndexer i: // we have to do this triage because dotnet is limited in terms of covariance
                        ((ICodeElementWriter<CodeIndexer>) elementWriter).WriteCodeElement(i, this);
                        break;
                    case ClassDeclaration d:
                        ((ICodeElementWriter<ClassDeclaration>) elementWriter).WriteCodeElement(d, this);
                        break;
                    case BlockEnd i:
                        ((ICodeElementWriter<BlockEnd>) elementWriter).WriteCodeElement(i, this);
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
                    case CodeNamespace n:
                        ((ICodeElementWriter<CodeNamespace>) elementWriter).WriteCodeElement(n, this);
                        break;
                    case CodeFunction n:
                        ((ICodeElementWriter<CodeFunction>) elementWriter).WriteCodeElement(n, this);
                        break;
                    case InterfaceDeclaration itfd:
                        ((ICodeElementWriter<InterfaceDeclaration>) elementWriter).WriteCodeElement(itfd, this);
                        break;
                }
            else if(code is not CodeClass && 
                    code is not BlockDeclaration &&
                    code is not BlockEnd &&
                    code is not CodeInterface)
                throw new InvalidOperationException($"Dispatcher missing for type {code.GetType()}");
        }
        protected void AddOrReplaceCodeElementWriter<T>(ICodeElementWriter<T> writer) where T: CodeElement {
            if (!Writers.TryAdd(typeof(T), writer))
                Writers[typeof(T)] = writer;
        }
        private readonly Dictionary<Type, object> Writers = new(); // we have to type as object because dotnet doesn't have type capture i.e eq for `? extends CodeElement`
        public static LanguageWriter GetLanguageWriter(GenerationLanguage language, string outputPath, string clientNamespaceName, bool usesBackingStore = false) {
            return language switch
            {
                GenerationLanguage.CSharp => new CSharpWriter(outputPath, clientNamespaceName),
                GenerationLanguage.Java => new JavaWriter(outputPath, clientNamespaceName),
                GenerationLanguage.TypeScript => new TypeScriptWriter(outputPath, clientNamespaceName, usesBackingStore),
                GenerationLanguage.Ruby => new RubyWriter(outputPath, clientNamespaceName),
                GenerationLanguage.PHP => new PhpWriter(outputPath, clientNamespaceName),
                GenerationLanguage.Go => new GoWriter(outputPath, clientNamespaceName),
                GenerationLanguage.Shell => new ShellWriter(outputPath, clientNamespaceName),
                GenerationLanguage.Swift => new SwiftWriter(outputPath, clientNamespaceName),
                _ => throw new InvalidEnumArgumentException($"{language} language currently not supported."),
            };
        }
    }
}
