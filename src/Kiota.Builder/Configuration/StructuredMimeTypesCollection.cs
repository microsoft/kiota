using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kiota.Builder.Configuration;

internal partial class StructuredMimeTypesCollection : IEnumerable<string>
{
    [GeneratedRegex(@"(?<mime>[^;]+);?q?=?(?<priority>[\d.]+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, 2000)]
    private static partial Regex mimeTypesRegex();
    private readonly static Regex mimeTypesRegexInstance = mimeTypesRegex();
    private readonly Dictionary<string, float> _mimeTypes;
    public StructuredMimeTypesCollection(IEnumerable<string> mimeTypes)
    {
        ArgumentNullException.ThrowIfNull(mimeTypes);
        _mimeTypes = mimeTypes.Select(static x => mimeTypesRegexInstance.Match(x))
                                .Where(static x => x.Success)
                                .Select(static x => (MimeType: x.Groups["mime"].Value, Priority: x.Groups["priority"].Success && float.TryParse(x.Groups["priority"].Value, CultureInfo.InvariantCulture, out var result) ? result : 1))
                                .ToDictionary(static x => x.MimeType, static x => x.Priority, StringComparer.OrdinalIgnoreCase);
        if (!_mimeTypes.Any())
            throw new ArgumentException("No valid mime types were provided", nameof(mimeTypes));
    }
    public IEnumerator<string> GetEnumerator()
    {
        return _mimeTypes.OrderByDescending(static x => x.Value).Select(static x => $"{x.Key};q={x.Value}").GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    public bool Contains(string mimeType)
    {
        return _mimeTypes.ContainsKey(mimeType);
    }
    public float? GetPriority(string mimeType)
    {
        return _mimeTypes.TryGetValue(mimeType, out var priority) ? priority : null;
    }
}
