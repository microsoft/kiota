using System;
using System.Collections.Generic;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.AL;

internal static class CodeElementExtensions
{
    public static bool ParentIsSkipped(this CodeElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(element.Parent);
        if (element.Parent is not IDocumentedElement)
            return false;
        return ((IDocumentedElement)element.Parent)?.GetCustomProperty("skip") == "true";
    }
    public static Dictionary<string, int> GetTypeCounts(this CodeElement codeElement)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        var typeCounts = new Dictionary<string, int>();
        foreach (var childElement in codeElement.GetChildElements())
        {
            if (typeCounts.TryGetValue(childElement.Name, out var value))
                typeCounts[childElement.GetType().Name] = ++value;
            else
                typeCounts[childElement.GetType().Name] = 1;
            var childTypeCounts = GetTypeCounts(childElement);
            foreach (var childTypeCount in childTypeCounts)
            {
                if (typeCounts.TryGetValue(childTypeCount.Key, out var childValue))
                    typeCounts[childTypeCount.Key] = childValue + childTypeCount.Value;
                else
                    typeCounts[childTypeCount.Key] = childTypeCount.Value;
            }
        }
        return typeCounts;
    }
}