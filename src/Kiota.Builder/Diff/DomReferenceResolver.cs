using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Diff;


public class DomReferenceResolver : ReferenceResolver
{
    private uint _referenceCount;
    private readonly Dictionary<string, object> _referenceIdToObjectMap = [];
    private readonly Dictionary<object, string> _objectToReferenceIdMap = [];
    public override void AddReference(string referenceId, object value)
    {
        if (!_referenceIdToObjectMap.TryAdd(referenceId, value))
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
        if (_objectToReferenceIdMap.TryGetValue(value, out var referenceId))
        {
            alreadyExists = true;
            return referenceId;
        }
        else
        {
            if (value is ICodeElement codeElement)
            {
                referenceId = GetReferenceId(codeElement);
            }
            else if (value is IDictionary<string, CodeNamespace> dictionaryNS && dictionaryNS.Values.FirstOrDefault() is { Parent: not null } firstNS)
            {
                referenceId = $"{GetReferenceId(firstNS.Parent)}-namespaces";
            }
            else if (value is IDictionary<string, CodeInterface> dictionaryInt && dictionaryInt.Values.FirstOrDefault() is { Parent: not null } firstInterface)
            {
                referenceId = $"{GetReferenceId(firstInterface.Parent)}-interfaces";
            }
            else if (value is IDictionary<string, CodeClass> dictionaryClasses && dictionaryClasses.Values.FirstOrDefault() is { Parent: not null } firstClass)
            {
                referenceId = $"{GetReferenceId(firstClass.Parent)}-classes";
            }
            else if (value is IDictionary<string, CodeEnum> dictionaryEnums && dictionaryEnums.Values.FirstOrDefault() is { Parent: not null } firstEnum)
            {
                referenceId = $"{GetReferenceId(firstEnum.Parent)}-enums";
            }
            else if (value is IDictionary<string, CodeFunction> dictionaryFunctions && dictionaryFunctions.Values.FirstOrDefault() is { Parent: not null } firstFunction)
            {
                referenceId = $"{GetReferenceId(firstFunction.Parent)}-functions";
            }
            else if (value is IDictionary<string, CodeConstant> dictionaryConstants && dictionaryConstants.Values.FirstOrDefault() is { Parent: not null } firstConstant)
            {
                referenceId = $"{GetReferenceId(firstConstant.Parent)}-constants";
            }
            else if (value is IDictionary<string, CodeParameter> dictionaryParameters && dictionaryParameters.Values.FirstOrDefault() is { Parent: not null } firstParameter)
            {
                referenceId = $"{GetReferenceId(firstParameter.Parent)}-parameters";
            }
            else if (value is IDictionary<string, CodeProperty> dictionaryProperties && dictionaryProperties.Values.FirstOrDefault() is { Parent: not null } firstProperty)
            {
                referenceId = $"{GetReferenceId(firstProperty.Parent)}-properties";
            }
            else if (value is IDictionary<string, CodeMethod> dictionaryMethods && dictionaryMethods.Values.FirstOrDefault() is { Parent: not null } firstMethod)
            {
                referenceId = $"{GetReferenceId(firstMethod.Parent)}-methods";
            }
            else if (value is IDictionary<string, CodeType> dictionaryTypes && dictionaryTypes.Values.FirstOrDefault() is { Parent: not null } firstType)
            {
                referenceId = $"{GetReferenceId(firstType.Parent)}-types";
            }
            else if (value is IDictionary<string, CodeTypeBase> dictionaryTypesBase && dictionaryTypesBase.Values.FirstOrDefault() is { Parent: not null } firstTypeBase)
            {
                referenceId = $"{GetReferenceId(firstTypeBase.Parent)}-typesBase";
            }
            else if (value is IDictionary<string, CodeEnumOption> dictionaryOption && dictionaryOption.Values.FirstOrDefault() is { Parent: not null } firstOption)
            {
                referenceId = $"{GetReferenceId(firstOption.Parent)}-options";
            }
            else if (value is IDictionary<string, CodeFile> dictionaryFile && dictionaryFile.Values.FirstOrDefault() is { Parent: not null } firstFile)
            {
                referenceId = $"{GetReferenceId(firstFile.Parent)}-files";
            }
            else
            {
                _referenceCount++;
                referenceId = _referenceCount.ToString(CultureInfo.InvariantCulture);
            }
            _objectToReferenceIdMap.Add(value, referenceId);
            alreadyExists = false;
            return referenceId;
        }
    }

    public override object ResolveReference(string referenceId)
    {
        if (_referenceIdToObjectMap.TryGetValue(referenceId, out var valueObject))
        {
            return valueObject;
        }
        else
        {
            throw new InvalidOperationException($"Reference id {referenceId} not found");
        }

    }
}
