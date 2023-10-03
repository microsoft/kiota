using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kiota.Builder.Configuration;

internal partial class StructuredMimeTypesCollection : ICollection<string>
{
    [GeneratedRegex(@"(?<mime>[^;]+);?q?=?(?<priority>[\d.]+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, 2000)]
    private static partial Regex mimeTypesRegex();
    private readonly static Regex mimeTypesRegexInstance = mimeTypesRegex();
    private readonly Dictionary<string, float> _mimeTypes;

    public int Count => _mimeTypes.Count;

    public bool IsReadOnly => false;
    public StructuredMimeTypesCollection()
    {
        _mimeTypes = new(StringComparer.OrdinalIgnoreCase);
    }
    public StructuredMimeTypesCollection(IEnumerable<string> mimeTypes)
    {
        ArgumentNullException.ThrowIfNull(mimeTypes);
        _mimeTypes = mimeTypes.Select(GetKeyAndPriority)
                                .OfType<KeyValuePair<string, float>>()
                                .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
        if (!_mimeTypes.Any())
            throw new ArgumentException("No valid mime types were provided", nameof(mimeTypes));
    }
    private static KeyValuePair<string, float>? GetKeyAndPriority(string rawFormat)
    {
        var match = mimeTypesRegexInstance.Match(rawFormat);
        if (match.Success)
        {
            var priority = match.Groups["priority"].Success && float.TryParse(match.Groups["priority"].Value, CultureInfo.InvariantCulture, out var resultPriority) ? resultPriority : 1;
            return new KeyValuePair<string, float>(match.Groups["mime"].Value, priority);
        }
        else
        {
            return null;
        }
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

    public void Add(string item)
    {
        if (GetKeyAndPriority(item) is { } result)
            _mimeTypes.TryAdd(result.Key, result.Value);
    }

    public void Clear()
    {
        _mimeTypes.Clear();
    }

    public void CopyTo(string[] array, int arrayIndex)
    {
        _mimeTypes.OrderByDescending(static x => x.Value).Select(static x => $"{x.Key};q={x.Value}").ToArray().CopyTo(array, arrayIndex);
    }

    public bool Remove(string item)
    {
        if (GetKeyAndPriority(item) is { } result && _mimeTypes.ContainsKey(result.Key))
        {
            _mimeTypes.Remove(result.Key);
            return true;
        }
        else
            return false;
    }
}
