﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
        if (codeClasses == null || codeClasses.Any(x => x == null))
            throw new ArgumentNullException(nameof(codeClasses));
        if (!codeClasses.Any())
            throw new ArgumentOutOfRangeException(nameof(codeClasses));
        return AddRange(codeClasses);
    }
    private static readonly char namespaceNameSeparator = '.';
    public CodeNamespace GetRootNamespace()
    {
        if (Parent is CodeNamespace parentNS) return parentNS.GetRootNamespace();
        return this;
    }
    public IEnumerable<CodeNamespace> Namespaces => InnerChildElements.Values.OfType<CodeNamespace>();
    public IEnumerable<CodeClass> Classes => InnerChildElements.Values.OfType<CodeClass>();
    public IEnumerable<CodeEnum> Enums => InnerChildElements.Values.OfType<CodeEnum>();
    public CodeNamespace? FindNamespaceByName(string nsName)
    {
        ArgumentException.ThrowIfNullOrEmpty(nsName);
        if (nsName.Equals(Name)) return this;
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
        var namespaceNameSegments = namespaceName.Split(namespaceNameSeparator, StringSplitOptions.RemoveEmptyEntries);
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
        if (enumDeclarations == null || enumDeclarations.Any(x => x == null))
            throw new ArgumentNullException(nameof(enumDeclarations));
        if (!enumDeclarations.Any())
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
        if (globalFunctions == null || globalFunctions.Any(x => x == null))
            throw new ArgumentNullException(nameof(globalFunctions));
        if (!globalFunctions.Any())
            throw new ArgumentOutOfRangeException(nameof(globalFunctions));
        return AddRange(globalFunctions);
    }
    public IEnumerable<CodeInterface> AddInterface(params CodeInterface[] interfaces)
    {
        if (interfaces == null || interfaces.Any(x => x == null))
            throw new ArgumentNullException(nameof(interfaces));
        if (!interfaces.Any())
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
            if (currentNamespaceSegments.ElementAt(deeperMostSegmentIndex).Equals(importNamespaceSegments.ElementAt(deeperMostSegmentIndex), StringComparison.OrdinalIgnoreCase))
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
}
