using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Kiota.Builder.Configuration;

public partial class StructuredMimeTypesCollection : ICollection<string>
{
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
    private static Func<string, bool> isPriorityParameterName = static x => x.Equals("q", StringComparison.OrdinalIgnoreCase);
    private static KeyValuePair<string, float>? GetKeyAndPriority(string rawFormat)
    {
        if (string.IsNullOrEmpty(rawFormat))
            return null;
        if (MediaTypeHeaderValue.TryParse(rawFormat, out var parsedFormat) && parsedFormat.MediaType is not null)
        {
            var priority = parsedFormat.Parameters.FirstOrDefault(static x => isPriorityParameterName(x.Name)) is { } priorityParameter && float.TryParse(priorityParameter.Value, CultureInfo.InvariantCulture, out var resultPriority) ? resultPriority : 1;
            return new KeyValuePair<string, float>(formatMediaType(parsedFormat), priority);
        }
        else
        {
            return null;
        }
    }
    private static string formatMediaType(MediaTypeHeaderValue value)
    {
        var additionalParameters = string.Join(";", value.Parameters.Where(static x => !isPriorityParameterName(x.Name)).Select(static x => $"{x.Name}={x.Value}"));
        var mediaType = string.IsNullOrEmpty(value.MediaType) ? "*/*" : value.MediaType;
        return string.IsNullOrEmpty(additionalParameters) ?
                    mediaType :
                    $"{mediaType};{additionalParameters}";
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
        return TryGetMimeType(mimeType, out var priority) ? priority : null;
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
                        .Select(x => TryGetMimeType(x, out var result) ? NormalizeMimeType(x, result) : null)
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
                        .Select(x => TryGetMimeType(x, out var result) ? new KeyValuePair<string, float>?(new(x, result)) : null)
                        .OfType<KeyValuePair<string, float>>()
                        .OrderByDescending(static x => x.Value)
                        .ThenByDescending(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(static x => x.Key);
    }
    [GeneratedRegex(@"[^/+]+\+", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline, 2000)]
    private static partial Regex vendorStripRegex();
    private readonly static Regex vendorStripRegexInstance = vendorStripRegex();
    private bool TryGetMimeType(string mimeType, out float result)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            result = default;
            return false;
        }

        return _mimeTypes.TryGetValue(mimeType, out result) ||
            mimeType.Contains('+', StringComparison.OrdinalIgnoreCase) &&
            _mimeTypes.TryGetValue(vendorStripRegexInstance.Replace(mimeType, string.Empty), out result);
    }
}
