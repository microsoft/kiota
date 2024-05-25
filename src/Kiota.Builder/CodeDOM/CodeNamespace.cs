using System;
using System.Collections.Generic;

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
    public override string Name
    {
        get => name;
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
    public IEnumerable<CodeNamespace> Namespaces
    {
        get
        {
            foreach (var item in InnerChildElements.Values)
            {
                if (item is CodeNamespace codeNamespace)
                {
                    yield return codeNamespace;
                }
            }
        }
    }

    public IEnumerable<CodeClass> Classes
    {
        get
        {
            foreach (var item in InnerChildElements.Values)
            {
                if (item is CodeClass codeClass)
                {
                    yield return codeClass;
                }
            }
        }
    }

    public IEnumerable<CodeEnum> Enums
    {
        get
        {
            foreach (var item in InnerChildElements.Values)
            {
                if (item is CodeEnum codeEnum)
                {
                    yield return codeEnum;
                }
            }
        }
    }

    public IEnumerable<CodeFunction> Functions
    {
        get
        {
            foreach (var item in InnerChildElements.Values)
            {
                if (item is CodeFunction codeFunction)
                {
                    yield return codeFunction;
                }
            }
        }
    }

    public IEnumerable<CodeInterface> Interfaces
    {
        get
        {
            foreach (var item in InnerChildElements.Values)
            {
                if (item is CodeInterface codeInterface)
                {
                    yield return codeInterface;
                }
            }
        }
    }

    public IEnumerable<CodeConstant> Constants
    {
        get
        {
            foreach (var item in InnerChildElements.Values)
            {
                if (item is CodeConstant codeConstant)
                {
                    yield return codeConstant;
                }
            }
        }
    }

    public IEnumerable<CodeFile> Files
    {
        get
        {
            foreach (var item in InnerChildElements.Values)
            {
                if (item is CodeFile codeFile)
                {
                    yield return codeFile;
                }
            }
        }
    }
    public CodeNamespace? FindNamespaceByName(string nsName)
    {
        ArgumentException.ThrowIfNullOrEmpty(nsName);
        if (nsName.Equals(Name, StringComparison.OrdinalIgnoreCase)) return this;
        var result = FindChildByName<CodeNamespace>(nsName, false);
        if (result == null)
        {
            foreach (var childElement in InnerChildElements.Values)
            {
                if (childElement is CodeNamespace childNS)
                {
                    result = childNS.FindNamespaceByName(nsName);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
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
            var segmentName = string.Empty;
            for (int i = 0; i <= lastPresentSegmentIndex; i++)
            {
                segmentName += (i > 0 ? "." : "") + namespaceNameSegments[i];
            }

            var segmentNameSpace = lastPresentSegmentNamespace.FindNamespaceByName(segmentName);
            if (segmentNameSpace is not null)
                lastPresentSegmentNamespace = segmentNameSpace;
            else
                break;
            lastPresentSegmentIndex++;
        }
        for (int i = lastPresentSegmentIndex; i < namespaceNameSegments.Length; i++)
        {
            var childSegment = namespaceNameSegments[i];
            var newNamespace = new CodeNamespace
            {
                Name = $"{lastPresentSegmentNamespace?.Name}{(string.IsNullOrEmpty(lastPresentSegmentNamespace?.Name) ? string.Empty : ".")}{childSegment}",
                Parent = lastPresentSegmentNamespace,
                IsItemNamespace = childSegment.Equals(ItemNamespaceName, StringComparison.OrdinalIgnoreCase)
            };
            lastPresentSegmentNamespace?.AddRange(newNamespace);
            lastPresentSegmentNamespace = newNamespace;
        }
        return lastPresentSegmentNamespace;
    }
    private const string ItemNamespaceName = "item";
    public bool IsItemNamespace
    {
        get; private set;
    }
    public CodeNamespace EnsureItemNamespace()
    {
        if (IsItemNamespace) return this;
        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("adding an item namespace at the root is not supported");

        CodeNamespace? childNamespace = null;
        foreach (var childElement in InnerChildElements.Values)
        {
            if (childElement is CodeNamespace codeNamespace && codeNamespace.IsItemNamespace)
            {
                childNamespace = codeNamespace;
                break;
            }
        }

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

        var downwardsSegments = new List<string>();
        for (int i = deeperMostSegmentIndex; i < importNamespaceSegmentsCount; i++)
        {
            downwardsSegments.Add(importNamespaceSegments[i]);
        }

        return new()
        { // we're in a parent namespace and need to import with a relative path or we're in a sub namespace and need to go "up" with dot dots
            UpwardsMovesCount = upMoves,
            DownwardsSegments = downwardsSegments
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
