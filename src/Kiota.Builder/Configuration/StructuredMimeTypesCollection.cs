using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Kiota.Builder.Configuration;

public partial class StructuredMimeTypesCollection : ICollection<string>
{
    private readonly Dictionary<string, float> _mimeTypes;
    public int Count => _mimeTypes.Count;
    public bool IsReadOnly => false;
    public StructuredMimeTypesCollection() : this([]) { }
    public StructuredMimeTypesCollection(IEnumerable<string> mimeTypes)
    {
        ArgumentNullException.ThrowIfNull(mimeTypes);

        _mimeTypes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var mimeType in mimeTypes)
        {
            var keyAndPriority = GetKeyAndPriority(mimeType);
            if (keyAndPriority is KeyValuePair<string, float> pair)
            {
                _mimeTypes[pair.Key] = pair.Value;
            }
        }
    }
    private static readonly Func<string, bool> isPriorityParameterName = static x => x.Equals("q", StringComparison.OrdinalIgnoreCase);
    private static KeyValuePair<string, float>? GetKeyAndPriority(string rawFormat)
    {
        if (!string.IsNullOrEmpty(rawFormat) && MediaTypeHeaderValue.TryParse(rawFormat, out var parsedFormat) && parsedFormat.MediaType is not null)
        {
            float priority = 1;
            foreach (var parameter in parsedFormat.Parameters)
            {
                if (isPriorityParameterName(parameter.Name))
                {
                    if (float.TryParse(parameter.Value, CultureInfo.InvariantCulture, out var resultPriority))
                    {
                        priority = resultPriority;
                    }
                    break;
                }
            }
            return new KeyValuePair<string, float>(formatMediaType(parsedFormat), priority);
        }
        throw new ArgumentException($"The provided media type {rawFormat} is not valid");
    }
    private static string formatMediaType(MediaTypeHeaderValue value)
    {
        var additionalParameters = new List<string>();
        foreach (var parameter in value.Parameters)
        {
            if (!isPriorityParameterName(parameter.Name))
            {
                additionalParameters.Add($"{parameter.Name}={parameter.Value}");
            }
        }

        var additionalParametersString = string.Join(";", additionalParameters);
        var mediaType = string.IsNullOrEmpty(value.MediaType) ? "*/*" : value.MediaType;

        return string.IsNullOrEmpty(additionalParametersString) ?
                        mediaType :
                        $"{mediaType};{additionalParametersString}";
    }
    public IEnumerator<string> GetEnumerator()
    {
        var sortedMimeTypes = new List<KeyValuePair<string, float>>(_mimeTypes);
        sortedMimeTypes.Sort((x, y) => y.Value.CompareTo(x.Value));

        var normalizedMimeTypes = new List<string>();
        foreach (var mimeType in sortedMimeTypes)
        {
            normalizedMimeTypes.Add(NormalizeMimeType(mimeType));
        }

        return normalizedMimeTypes.GetEnumerator();
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
        ArgumentNullException.ThrowIfNull(array);

        var sortedMimeTypes = new List<KeyValuePair<string, float>>(_mimeTypes);
        sortedMimeTypes.Sort((x, y) => y.Value.CompareTo(x.Value));

        int i = arrayIndex;
        foreach (var mimeType in sortedMimeTypes)
        {
            array[i] = NormalizeMimeType(mimeType);
            i++;
        }
    }
    private static string NormalizeMimeType(KeyValuePair<string, float> mimeType)
    {
        return NormalizeMimeType(mimeType.Key, mimeType.Value);
    }
    private static string NormalizeMimeType(string key, float value)
    {
        if (value == 1)
            return key;
        else
            return string.Create(CultureInfo.InvariantCulture, $"{key};q={value}");
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

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<string>();

        foreach (var searchType in searchTypes)
        {
            var keyAndPriority = GetKeyAndPriority(searchType);
            if (keyAndPriority is KeyValuePair<string, float> pair)
            {
                if (keys.Add(pair.Key))
                {
                    if (TryGetMimeType(pair.Key, out var result))
                    {
                        results.Add(NormalizeMimeType(pair.Key, result));
                    }
                }
            }
        }

        results.Sort(StringComparer.OrdinalIgnoreCase.Compare);
        return results;
    }
    public IEnumerable<string> GetContentTypes(IEnumerable<string> searchTypes)
    {
        ArgumentNullException.ThrowIfNull(searchTypes);

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pairs = new List<KeyValuePair<string, float>>();

        foreach (var searchType in searchTypes)
        {
            var keyAndPriority = GetKeyAndPriority(searchType);
            if (keyAndPriority is KeyValuePair<string, float> pair)
            {
                if (keys.Add(pair.Key))
                {
                    if (TryGetMimeType(pair.Key, out var result))
                    {
                        pairs.Add(new KeyValuePair<string, float>(pair.Key, result));
                    }
                }
            }
        }

        pairs.Sort((x, y) => y.Value != x.Value ? y.Value.CompareTo(x.Value) : StringComparer.OrdinalIgnoreCase.Compare(y.Key, x.Key));

        var results = new List<string>();
        foreach (var pair in pairs)
        {
            results.Add(pair.Key);
        }

        return results;
    }
    [GeneratedRegex(@"[^/+]+\+", RegexOptions.IgnoreCase | RegexOptions.Singleline, 2000)]
    private static partial Regex vendorStripRegex();
    private bool TryGetMimeType(string mimeType, out float result)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            result = default;
            return false;
        }

        return _mimeTypes.TryGetValue(mimeType, out result) || // vendor and parameters
            mimeType.Contains('+', StringComparison.OrdinalIgnoreCase) &&
            _mimeTypes.TryGetValue(vendorStripRegex().Replace(mimeType, string.Empty), out result) || // no vendor with parameters
            mimeType.Contains(';', StringComparison.OrdinalIgnoreCase) &&
            mimeType.Split(';', StringSplitOptions.RemoveEmptyEntries)[0] is string noParametersMimeType &&
            (_mimeTypes.TryGetValue(noParametersMimeType, out result) || // vendor without parameters
            _mimeTypes.TryGetValue(vendorStripRegex().Replace(noParametersMimeType, string.Empty), out result)); // no vendor without parameters
    }
}
