using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kiota.Builder.Configuration;

public partial class StructuredMimeTypesCollection : ICollection<string>
{
    [GeneratedRegex(@"(?<mime>[^;]+);?q?=?(?<priority>[\d.]+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, 2000)]
    private static partial Regex mimeTypesRegex();
    private readonly static Regex mimeTypesRegexInstance = mimeTypesRegex();
    private readonly Dictionary<string, float> _mimeTypes;

    public int Count => _mimeTypes.Count;

    public bool IsReadOnly => false;
    public StructuredMimeTypesCollection() : this(Array.Empty<string>()) { }
    public StructuredMimeTypesCollection(IEnumerable<string> mimeTypes)
    {
        ArgumentNullException.ThrowIfNull(mimeTypes);
        _mimeTypes = mimeTypes.Select(GetKeyAndPriority)
                                .OfType<KeyValuePair<string, float>>()
                                .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
    private static KeyValuePair<string, float>? GetKeyAndPriority(string rawFormat)
    {
        if (string.IsNullOrEmpty(rawFormat))
            return null;
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
        return _mimeTypes.OrderByDescending(static x => x.Value).Select(NormalizeMimeType).GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    public bool Contains(string item)
    {
        if (string.IsNullOrEmpty(item))
            return false;
        return _mimeTypes.ContainsKey(item);
    }
    public float? GetPriority(string mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return null;
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

    /// <inheritdoc/>
    public void CopyTo(string[] array, int arrayIndex)
    {
        _mimeTypes.OrderByDescending(static x => x.Value).Select(NormalizeMimeType).ToArray().CopyTo(array, arrayIndex);
    }
    private static string NormalizeMimeType(KeyValuePair<string, float> mimeType)
    {
        return NormalizeMimeType(mimeType.Key, mimeType.Value);
    }
    private static string NormalizeMimeType(string key, float value)
    {
        return FormattableString.Invariant($"{key};q={value}");
    }
    ///<inheritdoc/>
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
    public IEnumerable<string> GetAcceptedTypes(IEnumerable<string> searchTypes)
    {
        ArgumentNullException.ThrowIfNull(searchTypes);
        return searchTypes.Select(GetKeyAndPriority)
                        .OfType<KeyValuePair<string, float>>()
                        .Select(static x => x.Key)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(x => _mimeTypes.TryGetValue(x, out var result) ? NormalizeMimeType(x, result) : null)
                        .OfType<string>()
                        .Order(StringComparer.OrdinalIgnoreCase);
    }
    public IEnumerable<string> GetContentTypes(IEnumerable<string> searchTypes)
    {
        ArgumentNullException.ThrowIfNull(searchTypes);
        return searchTypes.Select(GetKeyAndPriority)
                        .OfType<KeyValuePair<string, float>>()
                        .Select(static x => x.Key)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(x => _mimeTypes.TryGetValue(x, out var result) ? new KeyValuePair<string, float>?(new(x, result)) : null)
                        .OfType<KeyValuePair<string, float>>()
                        .OrderByDescending(static x => x.Value)
                        .ThenByDescending(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(static x => x.Key);
    }
}
