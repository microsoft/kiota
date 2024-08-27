using System;
using System.Collections.Generic;

namespace Kiota.Builder.CodeDOM;

public record DeprecationInformation(string? DescriptionTemplate, DateTimeOffset? Date = null, DateTimeOffset? RemovalDate = null, string? Version = "", bool IsDeprecated = true, Dictionary<string, CodeTypeBase>? TypeReferences = null)
{
    public string GetDescription(Func<CodeTypeBase, string> typeReferenceResolver, string? typeReferencePrefix = null, string? typeReferenceSuffix = null)
    {
        if (DescriptionTemplate is null)
            return string.Empty;
        return CodeDocumentation.GetDescriptionInternal(DescriptionTemplate, typeReferenceResolver, TypeReferences, typeReferencePrefix, typeReferenceSuffix);
    }
};
