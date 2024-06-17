using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// 
/// </summary>
public class CodeNamespace : CodeBlock<BlockDeclaration, BlockEnd>
{
    private CodeNamespace()
    {
    }
    public static CodeNamespace InitRootNamespace()
    {
        return new();
    }
    private string name = string.Empty;
    [JsonIgnore]
    public override string Name
    {
        get
        {
            return name;
        }
        set
        {
            name = value;
            StartBlock.Name = name;
        }
    }

    public CodeFile TryAddCodeFile(string fileName, params CodeElement[] children)
    {
        var file = FindChildByName<CodeFile>(fileName, false) ?? new CodeFile { Name = fileName };
        RemoveChildElement(children);
        RemoveChildElementByName(fileName);
        if (children is { Length: > 0 })
            file.AddElements(children);

        if (!file.IsChildOf(this, true))
            AddRange(file);

        return file;
    }

    public bool IsParentOf(CodeNamespace childNamespace)
    {
        ArgumentNullException.ThrowIfNull(childNamespace);
        if (this == childNamespace)
            return false;
        return childNamespace.Name.StartsWith(Name + ".", StringComparison.OrdinalIgnoreCase);
    }
    public IEnumerable<CodeClass> AddClass(params CodeClass[] codeClasses)
    {
        if (codeClasses == null || Array.Exists(codeClasses, static x => x == null))
            throw new ArgumentNullException(nameof(codeClasses));
        if (codeClasses.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(codeClasses));
        return AddRange(codeClasses);
    }
    private const char NamespaceNameSeparator = '.';
    public CodeNamespace GetRootNamespace()
    {
        if (Parent is CodeNamespace parentNS) return parentNS.GetRootNamespace();
        return this;
    }
    [JsonPropertyName("namespaces")]
    public IDictionary<string, CodeNamespace>? NamespacesJSON
    {
        get => InnerChildElements.Where(static x => x.Value is CodeNamespace).ToDictionary(static x => x.Key, static x => (CodeNamespace)x.Value) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeNamespace> Namespaces => InnerChildElements.Values.OfType<CodeNamespace>();
    [JsonPropertyName("classes")]
    public IDictionary<string, CodeClass>? ClassesJSON
    {
        get => InnerChildElements.Where(static x => x.Value is CodeClass).ToDictionary(static x => x.Key, static x => (CodeClass)x.Value) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeClass> Classes => InnerChildElements.Values.OfType<CodeClass>();
    [JsonPropertyName("enums")]
    public IDictionary<string, CodeEnum>? EnumsJSON
    {
        get => InnerChildElements.Where(static x => x.Value is CodeEnum).ToDictionary(static x => x.Key, static x => (CodeEnum)x.Value) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeEnum> Enums => InnerChildElements.Values.OfType<CodeEnum>();
    [JsonPropertyName("functions")]
    public IDictionary<string, CodeFunction>? FunctionsJSON
    {
        get => InnerChildElements.Where(static x => x.Value is CodeFunction).ToDictionary(static x => x.Key, static x => (CodeFunction)x.Value) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeFunction> Functions => InnerChildElements.Values.OfType<CodeFunction>();
    [JsonPropertyName("interfaces")]
    public IDictionary<string, CodeInterface>? InterfacesJSON
    {
        get => InnerChildElements.Where(static x => x.Value is CodeInterface).ToDictionary(static x => x.Key, static x => (CodeInterface)x.Value) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeInterface> Interfaces => InnerChildElements.Values.OfType<CodeInterface>();
    [JsonPropertyName("constants")]
    public IDictionary<string, CodeConstant>? ConstantsJSON
    {
        get => InnerChildElements.Where(static x => x.Value is CodeConstant).ToDictionary(static x => x.Key, static x => (CodeConstant)x.Value) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeConstant> Constants => InnerChildElements.Values.OfType<CodeConstant>();
    [JsonPropertyName("files")]
    public IDictionary<string, CodeFile>? FilesJSON
    {
        get => InnerChildElements.Where(static x => x.Value is CodeFile).ToDictionary(static x => x.Key, static x => (CodeFile)x.Value) is { Count: > 0 } expanded ? expanded : null;
    }
    [JsonIgnore]
    public IEnumerable<CodeFile> Files => InnerChildElements.Values.OfType<CodeFile>();
    public CodeNamespace? FindNamespaceByName(string nsName)
    {
        ArgumentException.ThrowIfNullOrEmpty(nsName);
        if (nsName.Equals(Name, StringComparison.OrdinalIgnoreCase)) return this;
        var result = FindChildByName<CodeNamespace>(nsName, false);
        if (result == null)
            foreach (var childNS in InnerChildElements.Values.OfType<CodeNamespace>())
            {
                result = childNS.FindNamespaceByName(nsName);
                if (result != null)
                    break;
            }
        return result;
    }
    public CodeNamespace FindOrAddNamespace(string nsName) => FindNamespaceByName(nsName) ?? AddNamespace(nsName);
    public CodeNamespace AddNamespace(string namespaceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(namespaceName);
        var namespaceNameSegments = namespaceName.Split(NamespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
        var lastPresentSegmentIndex = default(int);
        var lastPresentSegmentNamespace = GetRootNamespace();
        while (lastPresentSegmentIndex < namespaceNameSegments.Length)
        {
            var segmentNameSpace = lastPresentSegmentNamespace.FindNamespaceByName(namespaceNameSegments.Take(lastPresentSegmentIndex + 1).Aggregate(static (x, y) => $"{x}.{y}"));
            if (segmentNameSpace is not null)
                lastPresentSegmentNamespace = segmentNameSpace;
            else
                break;
            lastPresentSegmentIndex++;
        }
        foreach (var childSegment in namespaceNameSegments.Skip(lastPresentSegmentIndex))
            lastPresentSegmentNamespace = lastPresentSegmentNamespace
                                        .AddRange(
                                            new CodeNamespace
                                            {
                                                Name = $"{lastPresentSegmentNamespace?.Name}{(string.IsNullOrEmpty(lastPresentSegmentNamespace?.Name) ? string.Empty : ".")}{childSegment}",
                                                Parent = lastPresentSegmentNamespace,
                                                IsItemNamespace = childSegment.Equals(ItemNamespaceName, StringComparison.OrdinalIgnoreCase)
                                            }).First();
        return lastPresentSegmentNamespace;
    }
    private const string ItemNamespaceName = "item";
    [JsonIgnore]
    public bool IsItemNamespace
    {
        get; private set;
    }
    public CodeNamespace EnsureItemNamespace()
    {
        if (IsItemNamespace) return this;
        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("adding an item namespace at the root is not supported");
        var childNamespace = InnerChildElements.Values.OfType<CodeNamespace>().FirstOrDefault(x => x.IsItemNamespace);
        if (childNamespace == null)
        {
            childNamespace = AddNamespace($"{Name}.{ItemNamespaceName}");
            childNamespace.IsItemNamespace = true;
        }
        return childNamespace;
    }
    public IEnumerable<CodeEnum> AddEnum(params CodeEnum[] enumDeclarations)
    {
        if (enumDeclarations == null || Array.Exists(enumDeclarations, static x => x == null))
            throw new ArgumentNullException(nameof(enumDeclarations));
        if (enumDeclarations.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(enumDeclarations));
        return AddRange(enumDeclarations);
    }
    [JsonIgnore]
    public int Depth
    {
        get
        {
            if (Parent is CodeNamespace n) return n.Depth + 1;
            return 0;
        }
    }

    public IEnumerable<CodeFunction> AddFunction(params CodeFunction[] globalFunctions)
    {
        if (globalFunctions == null || Array.Exists(globalFunctions, static x => x == null))
            throw new ArgumentNullException(nameof(globalFunctions));
        if (globalFunctions.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(globalFunctions));
        return AddRange(globalFunctions);
    }
    public IEnumerable<CodeInterface> AddInterface(params CodeInterface[] interfaces)
    {
        if (interfaces == null || Array.Exists(interfaces, static x => x == null))
            throw new ArgumentNullException(nameof(interfaces));
        if (interfaces.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(interfaces));
        return AddRange(interfaces);
    }
    public NamespaceDifferentialTracker GetDifferential(CodeNamespace importNamespace, string namespacePrefix, char separator = '.')
    {
        ArgumentNullException.ThrowIfNull(importNamespace);
        ArgumentException.ThrowIfNullOrEmpty(namespacePrefix);
        if (this == importNamespace || Name.Equals(importNamespace.Name, StringComparison.OrdinalIgnoreCase)) // we're in the same namespace
            return new();
        var prefixLength = namespacePrefix.Length;
        var currentNamespaceSegments = Name[prefixLength..]
                                .Split(separator, StringSplitOptions.RemoveEmptyEntries);
        var importNamespaceSegments = importNamespace
                            .Name[prefixLength..]
                            .Split(separator, StringSplitOptions.RemoveEmptyEntries);
        var importNamespaceSegmentsCount = importNamespaceSegments.Length;
        var currentNamespaceSegmentsCount = currentNamespaceSegments.Length;
        var deeperMostSegmentIndex = 0;
        while (deeperMostSegmentIndex < Math.Min(importNamespaceSegmentsCount, currentNamespaceSegmentsCount))
        {
            if (currentNamespaceSegments[deeperMostSegmentIndex].Equals(importNamespaceSegments[deeperMostSegmentIndex], StringComparison.OrdinalIgnoreCase))
                deeperMostSegmentIndex++;
            else
                break;
        }
        var upMoves = currentNamespaceSegmentsCount - deeperMostSegmentIndex;
        return new()
        { // we're in a parent namespace and need to import with a relative path or we're in a sub namespace and need to go "up" with dot dots
            UpwardsMovesCount = upMoves,
            DownwardsSegments = importNamespaceSegments.Skip(deeperMostSegmentIndex)
        };
    }
    internal IEnumerable<CodeConstant> AddConstant(params CodeConstant[] codeConstants)
    {
        if (codeConstants == null || Array.Exists(codeConstants, static x => x == null))
            throw new ArgumentNullException(nameof(codeConstants));
        if (codeConstants.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(codeConstants));
        return AddRange(codeConstants);
    }
}
