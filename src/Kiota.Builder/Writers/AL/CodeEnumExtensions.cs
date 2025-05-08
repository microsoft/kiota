using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.AL;

internal static class CodeEnumExtensions
{
    public static IEnumerable<CodeEnumOption> ObjectProperties(this CodeEnum codeEnum)
    {
        ArgumentNullException.ThrowIfNull(codeEnum);
        return codeEnum.Options.Where(p1 => p1.IsObjectProperty());
    }
    public static IEnumerable<ALObjectProperty> ToObjectProperties(this IEnumerable<CodeEnumOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Select(p1 => new ALObjectProperty(p1.Name, p1.SerializationName));
    }
}