using System;
using System.Linq;
using kiota.core;

public static class OpenApiUrlSpaceNodeExtensions {
    public static bool DoesNodeBelongToItemSubnamespace(this OpenApiUrlSpaceNode node) =>
        (node?.Segment?.StartsWith("{") ?? false) && (node?.Segment?.EndsWith("}") ?? false);
    private static readonly char pathNameSeparator = '\\';
    public static string GetNodeNamespaceFromPath(this OpenApiUrlSpaceNode node, string prefix = default) =>
        prefix + 
                ((node?.Path?.Contains(pathNameSeparator) ?? false) ?
                    "." + node.Path
                            ?.Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                            ?.Where(x => !x.StartsWith('{'))
                            ?.Aggregate((x, y) => $"{x}.{y}") :
                    string.Empty)
                .ReplaceValueIdentifier();
    
    public static string GetClassName(this OpenApiUrlSpaceNode node, string suffix = default, string prefix = default) {
        var rawClassName = node?.Identifier?.ReplaceValueIdentifier();
        if(node.DoesNodeBelongToItemSubnamespace() && rawClassName.EndsWith("Id"))
            rawClassName = rawClassName.Substring(0, rawClassName.Length - 2);
        return prefix + rawClassName + suffix;
    }
}
