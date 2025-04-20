using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.AL;
public class ALWriter : LanguageWriter
{
    private readonly ALConventionService conventionService = new();
    public ALObjectIdProvider ObjectIdProvider
    {
        get;
    }
    public ALWriter(string rootPath, string clientNamespaceName)
    {
        PathSegmenter = new ALPathSegmenter(rootPath, clientNamespaceName);
        conventionService = new ALConventionService();
        ObjectIdProvider = new ALObjectIdProvider();
        AddOrReplaceCodeElementWriter(new CodeClassDeclarationWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeBlockEndWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeFunctionWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeIndexerWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeTypeWriter(conventionService));
    }
    internal void WriteVariablesDeclaration(IEnumerable<CodeMethod> properties, CodeClass codeClass, bool includePragma = true, bool combineVariables = true)
        => WriteVariablesDeclaration(properties.ToVariables(), codeClass.GetPragmasVariables(), includePragma, combineVariables);
    internal void WriteVariablesDeclaration(IEnumerable<CodeProperty> properties, CodeClass codeClass, bool includePragma = true, bool combineVariables = true)
        => WriteVariablesDeclaration(properties.ToVariables(), codeClass.GetPragmasVariables(), includePragma, combineVariables);
    internal void WriteVariablesDeclaration(IEnumerable<CodeParameter> properties, CodeMethod codeMethod, bool includePragma = true, bool combineVariables = true)
        => WriteVariablesDeclaration(properties.ToVariables(), codeMethod.GetPragmasVariables(), includePragma, combineVariables);
    internal void WriteVariablesDeclaration(IEnumerable<ALVariable> properties, string pragmasVariables, bool includePragma = true, bool combineVariables = true)
    {
        var propertyList = properties.ToList().OrderBy(x => x.Type.Name).ToList();
        if (properties is null)
            return;
        if (propertyList.Count == 0)
            return;
        if (combineVariables)
            properties = ALVariableProvider.CombineVariablesOfSameType(properties);

        StartBlock("var");
        WritePragmaConditionalDisable(pragmasVariables, includePragma);
        foreach (var property in properties.OrderBy(x => x.DefaultValue))
            property.Write(this);
        WritePragmaConditionalRestore(pragmasVariables, includePragma);
        DecreaseIndent();
    }
    internal void WriteObjectProperties(IEnumerable<ALObjectProperty> objectProperties)
    {
        foreach (var property in objectProperties)
            property.Write(this);
        WriteLine();
    }
    internal void WritePragmaConditionalDisable(string items, bool includePragma = true) => WritePragmaConditional("disable", items, includePragma);
    internal void WritePragmaConditionalRestore(string items, bool includePragma = true) => WritePragmaConditional("restore", items, includePragma);
    internal void WritePragmaConditional(string action, string items, bool includePragma = true)
    {
        if (String.IsNullOrEmpty(items))
            return;
        if (includePragma)
            WriteLine($"#pragma warning {action} {string.Join(", ", items)}");
    }
}
