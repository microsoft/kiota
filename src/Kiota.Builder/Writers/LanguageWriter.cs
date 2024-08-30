﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;
using Kiota.Builder.Writers.Cli;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.Java;
using Kiota.Builder.Writers.Php;
using Kiota.Builder.Writers.Python;
using Kiota.Builder.Writers.Ruby;
using Kiota.Builder.Writers.Swift;
using Kiota.Builder.Writers.TypeScript;

namespace Kiota.Builder.Writers;

public abstract class LanguageWriter
{
    private TextWriter? writer;
    private const int IndentSize = 4;
    private static readonly string indentString = Enumerable.Repeat(" ", 1000).Aggregate(static (x, y) => x + y);
    private int currentIndent;

    /// <summary>
    /// This method must be called before you can use the writer method
    /// </summary>
    /// <param name="writer"></param>
    /// <remarks>Passing this to the constructor is problematic because for writing to files, an instance of this
    /// class is needed to get the file suffix to be able to create the file stream to create the writer.
    /// By making this a separate step, we can instantiate the LanguageWriter, then get the suffix, then create the writer.</remarks>
    public void SetTextWriter(TextWriter writer)
    {
        this.writer = writer;
    }
    public IPathSegmenter? PathSegmenter
    {
        get; protected set;
    }

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
    public static string NewLine
    {
        get => Environment.NewLine;
    }
    /// <summary>
    /// Adds an empty line
    /// </summary>
    internal void WriteLine() => WriteLine(string.Empty, false);
    internal void WriteLine(string line, bool includeIndent = true)
    {
        writer?.WriteLine(includeIndent ? GetIndent() + line : line);
    }
    internal void WriteLines(IEnumerable<string> lines)
    {
        WriteLinesInternal(lines);
    }
    internal void WriteLines(params string[] lines)
    {
        WriteLinesInternal(lines);
    }
    private void WriteLinesInternal(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            WriteLine(line);
        }
    }
    internal void StartBlock(string symbol = "{", bool increaseIndent = true)
    {
        WriteLine(symbol);
        if (increaseIndent)
            IncreaseIndent();
    }
    internal void CloseBlock(string symbol = "}", bool decreaseIndent = true)
    {
        if (decreaseIndent)
            DecreaseIndent();
        WriteLine(symbol);
    }

    internal void WriteBlock(string startSymbol = "{", string closeSymbol = "}", params string[] lines)
    {
        StartBlock(startSymbol);
        WriteLines(lines);
        CloseBlock(closeSymbol);
    }

    internal void Write(string text, bool includeIndent = true)
    {
        writer?.Write(includeIndent ? GetIndent() + text : text);
    }
    /// <summary>
    /// Dispatch call to Write the code element to the proper derivative write method
    /// </summary>
    /// <param name="code"></param>
    public void Write<T>(T code) where T : CodeElement
    {
        ArgumentNullException.ThrowIfNull(code);
        if (Writers.TryGetValue(code.GetType(), out var elementWriter))
            switch (code)
            {
                case CodeProperty p when !p.ExistsInBaseType: // to avoid duplicating props on inheritance structure
                    ((ICodeElementWriter<CodeProperty>)elementWriter).WriteCodeElement(p, this);
                    break;
                case CodeIndexer i: // we have to do this triage because dotnet is limited in terms of covariance
                    ((ICodeElementWriter<CodeIndexer>)elementWriter).WriteCodeElement(i, this);
                    break;
                case ClassDeclaration d:
                    ((ICodeElementWriter<ClassDeclaration>)elementWriter).WriteCodeElement(d, this);
                    break;
                case CodeFileBlockEnd cfb:
                    ((ICodeElementWriter<CodeFileBlockEnd>)elementWriter).WriteCodeElement(cfb, this);
                    break;
                case BlockEnd i:
                    ((ICodeElementWriter<BlockEnd>)elementWriter).WriteCodeElement(i, this);
                    break;
                case CodeEnum e:
                    ((ICodeElementWriter<CodeEnum>)elementWriter).WriteCodeElement(e, this);
                    break;
                case CodeMethod m:
                    ((ICodeElementWriter<CodeMethod>)elementWriter).WriteCodeElement(m, this);
                    break;
                case CodeType t:
                    ((ICodeElementWriter<CodeType>)elementWriter).WriteCodeElement(t, this);
                    break;
                case CodeNamespace n:
                    ((ICodeElementWriter<CodeNamespace>)elementWriter).WriteCodeElement(n, this);
                    break;
                case CodeFunction n:
                    ((ICodeElementWriter<CodeFunction>)elementWriter).WriteCodeElement(n, this);
                    break;
                case InterfaceDeclaration itfd:
                    ((ICodeElementWriter<InterfaceDeclaration>)elementWriter).WriteCodeElement(itfd, this);
                    break;
                case CodeFileDeclaration cfd:
                    ((ICodeElementWriter<CodeFileDeclaration>)elementWriter).WriteCodeElement(cfd, this);
                    break;
                case CodeConstant codeConstant:
                    ((ICodeElementWriter<CodeConstant>)elementWriter).WriteCodeElement(codeConstant, this);
                    break;
                case CodeUnionType codeUnionType:
                    ((ICodeElementWriter<CodeUnionType>)elementWriter).WriteCodeElement(codeUnionType, this);
                    break;
                case CodeIntersectionType codeIntersectionType:
                    ((ICodeElementWriter<CodeIntersectionType>)elementWriter).WriteCodeElement(codeIntersectionType, this);
                    break;
            }
        else if (code is not CodeClass &&
                code is not BlockDeclaration &&
                code is not BlockEnd &&
                code is not CodeInterface &&
                code is not CodeFile &&
                code is not CodeEnumOption)
            throw new InvalidOperationException($"Dispatcher missing for type {code.GetType()}");
    }
    protected void AddOrReplaceCodeElementWriter<T>(ICodeElementWriter<T> writer) where T : CodeElement
    {
        if (!Writers.TryAdd(typeof(T), writer))
            Writers[typeof(T)] = writer;
    }
    private readonly Dictionary<Type, object> Writers = []; // we have to type as object because dotnet doesn't have type capture i.e eq for `? extends CodeElement`
    public static LanguageWriter GetLanguageWriter(GenerationLanguage language, string outputPath, string clientNamespaceName, bool usesBackingStore = false, bool excludeBackwardCompatible = false)
    {
        return language switch
        {
            GenerationLanguage.CSharp => new CSharpWriter(outputPath, clientNamespaceName),
            GenerationLanguage.Java => new JavaWriter(outputPath, clientNamespaceName),
            GenerationLanguage.TypeScript => new TypeScriptWriter(outputPath, clientNamespaceName),
            GenerationLanguage.Ruby => new RubyWriter(outputPath, clientNamespaceName),
            GenerationLanguage.PHP => new PhpWriter(outputPath, clientNamespaceName, usesBackingStore),
            GenerationLanguage.Python => new PythonWriter(outputPath, clientNamespaceName, usesBackingStore),
            GenerationLanguage.Go => new GoWriter(outputPath, clientNamespaceName, excludeBackwardCompatible),
            GenerationLanguage.CLI => new CliWriter(outputPath, clientNamespaceName),
            GenerationLanguage.Swift => new SwiftWriter(outputPath, clientNamespaceName),
            _ => throw new InvalidEnumArgumentException($"{language} language currently not supported."),
        };
    }
}
