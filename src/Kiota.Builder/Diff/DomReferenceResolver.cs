using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Diff;


public class DomReferenceResolver : ReferenceResolver
{
    private readonly Dictionary<string, ICodeElement> _referenceIdToCodeElementMap = [];
    private readonly Dictionary<ICodeElement, string> _codeElementToReferenceIdMap = [];
    private uint _referenceCount;
    private readonly Dictionary<string, object> _referenceIdToObjectMap = [];
    private readonly Dictionary<object, string> _objectToReferenceIdMap = [];
    public override void AddReference(string referenceId, object value)
    {
        if (value is ICodeElement codeElement)
        {
            if (!_referenceIdToCodeElementMap.TryAdd(referenceId, codeElement))
            {
                throw new InvalidOperationException($"Reference id {referenceId} already exists");
            }
        }
        else if (!_referenceIdToObjectMap.TryAdd(referenceId, value))
        {
            throw new InvalidOperationException($"Reference id {referenceId} already exists");
        }
    }
    private static string GetReferenceId(ICodeElement value)
    {
        if (value.Parent is not null)
            return $"{GetReferenceId(value.Parent)}.{value.Name}";
        if (string.IsNullOrEmpty(value.Name))
            return "root";
        return value.Name;
    }

    public override string GetReference(object value, out bool alreadyExists)
    {
        if (value is ICodeElement codeElement)
        {
            if (_codeElementToReferenceIdMap.TryGetValue(codeElement, out var referenceId))
            {
                alreadyExists = true;
                return referenceId;

            }
            else
            {
                referenceId = GetReferenceId(codeElement);
                _codeElementToReferenceIdMap.Add(codeElement, referenceId);
                alreadyExists = false;
            }
            return referenceId;
        }
        else if (_objectToReferenceIdMap.TryGetValue(value, out var referenceId))
        {
            alreadyExists = true;
            return referenceId;
        }
        else
        {
            _referenceCount++;
            referenceId = _referenceCount.ToString(CultureInfo.InvariantCulture);
            _objectToReferenceIdMap.Add(value, referenceId);
            alreadyExists = false;
            return referenceId;
        }
    }

    public override object ResolveReference(string referenceId)
    {
        if (_referenceIdToCodeElementMap.TryGetValue(referenceId, out var value))
        {
            return value;
        }
        else if (_referenceIdToObjectMap.TryGetValue(referenceId, out var valueObject))
        {
            return valueObject;
        }
        else
        {
            throw new InvalidOperationException($"Reference id {referenceId} not found");
        }

    }
}
