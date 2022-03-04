// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using System.Xml;

namespace Microsoft.Kiota.Serialization.Json
{
    /// <summary>
    /// The <see cref="IParseNode"/> implementation for the json content type
    /// </summary>
    public class JsonParseNode : IParseNode
    {
        private readonly JsonElement _jsonNode;
        /// <summary>
        /// The <see cref="JsonParseNode"/> constructor.
        /// </summary>
        /// <param name="node">The JsonElement to initialize the node with</param>
        public JsonParseNode(JsonElement node)
        {
            _jsonNode = node;
        }

        /// <summary>
        /// Get the string value from the json node
        /// </summary>
        /// <returns>A string value</returns>
        public string GetStringValue() => _jsonNode.GetString();

        /// <summary>
        /// Get the boolean value from the json node
        /// </summary>
        /// <returns>A boolean value</returns>
        public bool? GetBoolValue() => _jsonNode.GetBoolean();

        /// <summary>
        /// Get the int value from the json node
        /// </summary>
        /// <returns>A int value</returns>
        public int? GetIntValue() => _jsonNode.GetInt32();

        /// <summary>
        /// Get the float value from the json node
        /// </summary>
        /// <returns>A float value</returns>
        public float? GetFloatValue() => _jsonNode.GetSingle();

        /// <summary>
        /// Get the Long value from the json node
        /// </summary>
        /// <returns>A Long value</returns>
        public long? GetLongValue() => _jsonNode.GetInt64();

        /// <summary>
        /// Get the double value from the json node
        /// </summary>
        /// <returns>A double value</returns>
        public double? GetDoubleValue() => _jsonNode.GetDouble();

        /// <summary>
        /// Get the decimal value from the json node
        /// </summary>
        /// <returns>A decimal value</returns>
        public decimal? GetDecimalValue() => _jsonNode.GetDecimal();

        /// <summary>
        /// Get the guid value from the json node
        /// </summary>
        /// <returns>A guid value</returns>
        public Guid? GetGuidValue() => _jsonNode.GetGuid();

        /// <summary>
        /// Get the <see cref="DateTimeOffset"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="DateTimeOffset"/> value</returns>
        public DateTimeOffset? GetDateTimeOffsetValue() 
        {
            // JsonElement.GetDateTimeOffset is super strict so try to be more lenient if it fails(e.g. when we have whitespace or other variant formats).
            // ref - https://docs.microsoft.com/en-us/dotnet/standard/datetime/system-text-json-support
            if(!_jsonNode.TryGetDateTimeOffset(out var value))
                value = DateTimeOffset.Parse(_jsonNode.GetString());

            return value;
        }

        /// <summary>
        /// Get the <see cref="TimeSpan"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> value</returns>
        public TimeSpan? GetTimeSpanValue()
        {
            var jsonString = _jsonNode.GetString();
            if(string.IsNullOrEmpty(jsonString))
                return null;

            // Parse an ISO8601 duration.http://en.wikipedia.org/wiki/ISO_8601#Durations to a TimeSpan
            return XmlConvert.ToTimeSpan(jsonString);
        }

        /// <summary>
        /// Get the <see cref="Date"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="Date"/> value</returns>
        public Date? GetDateValue()
        {
            var dateString = _jsonNode.GetString();
            if(!DateTime.TryParse(dateString,out var result))
                return null;

            return new Date(result);
        }

        /// <summary>
        /// Get the <see cref="Time"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="Time"/> value</returns>
        public Time? GetTimeValue()
        {
            var dateString = _jsonNode.GetString();
            if(!DateTime.TryParse(dateString,out var result))
                return null;

            return new Time(result);
        }

        /// <summary>
        /// Get the enumeration value of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <returns>An enumeration value or null</returns>
        public T? GetEnumValue<T>() where T : struct, Enum
        {
            var rawValue = _jsonNode.GetString();
            if(string.IsNullOrEmpty(rawValue)) return null;
            if(typeof(T).GetCustomAttributes<FlagsAttribute>().Any())
            {
                return (T)(object)rawValue
                    .Split(',')
                    .Select(x => Enum.TryParse<T>(x, true, out var result) ? result : (T?)null)
                    .Where(x => !x.Equals(null))
                    .Select(x => (int)(object)x)
                    .Sum();
            }
            else
                return Enum.TryParse<T>(rawValue, true,out var result) ? result : null;
        }

        /// <summary>
        /// Get the collection of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <param name="factory">The factory to use to create the model object.</param>
        /// <returns>A collection of objects</returns>
        public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable
        {
            var enumerator = _jsonNode.EnumerateArray();
            while(enumerator.MoveNext())
            {
                var currentParseNode = new JsonParseNode(enumerator.Current)
                {
                    OnAfterAssignFieldValues = OnAfterAssignFieldValues,
                    OnBeforeAssignFieldValues = OnBeforeAssignFieldValues
                };
                yield return currentParseNode.GetObjectValue<T>(factory);
            }
        }
        /// <summary>
        /// Gets the collection of enum values of the node.
        /// </summary>
        /// <returns>The collection of enum values.</returns>
        public IEnumerable<T?> GetCollectionOfEnumValues<T>() where T : struct, Enum
        {
            var enumerator = _jsonNode.EnumerateArray();
            while(enumerator.MoveNext())
            {
                var currentParseNode = new JsonParseNode(enumerator.Current)
                {
                    OnAfterAssignFieldValues = OnAfterAssignFieldValues,
                    OnBeforeAssignFieldValues = OnBeforeAssignFieldValues
                };
                yield return currentParseNode.GetEnumValue<T>();
            }
        }
        /// <summary>
        /// Gets the byte array value of the node.
        /// </summary>
        /// <returns>The byte array value of the node.</returns>
        public byte[] GetByteArrayValue() {
            var rawValue = _jsonNode.GetString();
            if(string.IsNullOrEmpty(rawValue)) return null;
            return Convert.FromBase64String(rawValue);
        }
        private static Type booleanType = typeof(bool?);
        private static Type stringType = typeof(string);
        private static Type intType = typeof(int?);
        private static Type floatType = typeof(float?);
        private static Type doubleType = typeof(double?);
        private static Type guidType = typeof(Guid?);
        private static Type dateTimeOffsetType = typeof(DateTimeOffset?);
        private static Type timeSpanType = typeof(TimeSpan?);
        private static Type dateType = typeof(Date?);
        private static Type timeType = typeof(Time?);

        /// <summary>
        /// Get the collection of primitives of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <returns>A collection of objects</returns>
        public IEnumerable<T> GetCollectionOfPrimitiveValues<T>()
        {
            var genericType = typeof(T);
            foreach(var collectionValue in _jsonNode.EnumerateArray())
            {
                var currentParseNode = new JsonParseNode(collectionValue)
                {
                    OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
                    OnAfterAssignFieldValues = OnAfterAssignFieldValues
                };
                if(genericType == booleanType)
                    yield return (T)(object)currentParseNode.GetBoolValue();
                else if(genericType == stringType)
                    yield return (T)(object)currentParseNode.GetStringValue();
                else if(genericType == intType)
                    yield return (T)(object)currentParseNode.GetIntValue();
                else if(genericType == floatType)
                    yield return (T)(object)currentParseNode.GetFloatValue();
                else if(genericType == doubleType)
                    yield return (T)(object)currentParseNode.GetDoubleValue();
                else if(genericType == guidType)
                    yield return (T)(object)currentParseNode.GetGuidValue();
                else if(genericType == dateTimeOffsetType)
                    yield return (T)(object)currentParseNode.GetDateTimeOffsetValue();
                else if(genericType == timeSpanType)
                    yield return (T)(object)currentParseNode.GetTimeSpanValue();
                else if(genericType == dateType)
                    yield return (T)(object)currentParseNode.GetDateValue();
                else if(genericType == timeType)
                    yield return (T)(object)currentParseNode.GetTimeValue();
                else
                    throw new InvalidOperationException($"unknown type for deserialization {genericType.FullName}");
            }
        }

        /// <summary>
        /// The action to perform before assigning field values.
        /// </summary>
        public Action<IParsable> OnBeforeAssignFieldValues { get; set; }

        /// <summary>
        /// The action to perform after assigning field values.
        /// </summary>
        public Action<IParsable> OnAfterAssignFieldValues { get; set; }

        /// <summary>
        /// Get the object of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <param name="factory">The factory to use to create the model object.</param>
        /// <returns>A object of the specified type</returns>
        public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable
        {
            var item = factory(this);
            var fieldDeserializers = item.GetFieldDeserializers<T>();
            OnBeforeAssignFieldValues?.Invoke(item);
            AssignFieldValues(item, fieldDeserializers);
            OnAfterAssignFieldValues?.Invoke(item);
            return item;
        }
        private void AssignFieldValues<T>(T item, IDictionary<string, Action<T, IParseNode>> fieldDeserializers) where T : IParsable
        {
            if(_jsonNode.ValueKind != JsonValueKind.Object) return;
            IDictionary<string, object> itemAdditionalData = null;
            if(item is IAdditionalDataHolder holder)
            {
                if(holder.AdditionalData == null)
                    holder.AdditionalData = new Dictionary<string, object>();
                itemAdditionalData = holder.AdditionalData;
            }

            foreach(var fieldValue in _jsonNode.EnumerateObject())
            {
                if(fieldDeserializers.ContainsKey(fieldValue.Name))
                {
                    if(fieldValue.Value.ValueKind == JsonValueKind.Null)
                        continue;// If the property is already null just continue. As calling functions like GetDouble,GetBoolValue do not process JsonValueKind.Null.

                    var fieldDeserializer = fieldDeserializers[fieldValue.Name];
                    Debug.WriteLine($"found property {fieldValue.Name} to deserialize");
                    fieldDeserializer.Invoke(item, new JsonParseNode(fieldValue.Value)
                    {
                        OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
                        OnAfterAssignFieldValues = OnAfterAssignFieldValues
                    });
                }
                else if (itemAdditionalData != null)
                {
                    Debug.WriteLine($"found additional property {fieldValue.Name} to deserialize");
                    itemAdditionalData.TryAdd(fieldValue.Name, TryGetAnything(fieldValue.Value));
                }
                else
                {
                    Debug.WriteLine($"found additional property {fieldValue.Name} to deserialize but the model doesn't support additional data");
                }
            }
        }
        private object TryGetAnything(JsonElement element)
        {
            switch(element.ValueKind)
            {
                case JsonValueKind.Number:
                    if(element.TryGetDecimal(out var dec)) return dec;
                    else if(element.TryGetDouble(out var db)) return db;
                    else if(element.TryGetInt16(out var s)) return s;
                    else if(element.TryGetInt32(out var i)) return i;
                    else if(element.TryGetInt64(out var l)) return l;
                    else if(element.TryGetSingle(out var f)) return f;
                    else if(element.TryGetUInt16(out var us)) return us;
                    else if(element.TryGetUInt32(out var ui)) return ui;
                    else if(element.TryGetUInt64(out var ul)) return ul;
                    else throw new InvalidOperationException("unexpected additional value type during number deserialization");
                case JsonValueKind.String:
                    if(element.TryGetDateTime(out var dt)) return dt;
                    else if(element.TryGetDateTimeOffset(out var dto)) return dto;
                    else if(element.TryGetGuid(out var g)) return g;
                    else return element.GetString();
                case JsonValueKind.Array:
                case JsonValueKind.Object:
                    return element;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    throw new InvalidOperationException($"unexpected additional value type during deserialization json kind : {element.ValueKind}");
            }
        }

        /// <summary>
        /// Get the child node of the specified identifier
        /// </summary>
        /// <param name="identifier">The identifier of the child node</param>
        /// <returns>An instance of <see cref="IParseNode"/></returns>
        public IParseNode GetChildNode(string identifier) => new JsonParseNode(_jsonNode.GetProperty(identifier ?? throw new ArgumentNullException(nameof(identifier))))
        {
            OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
            OnAfterAssignFieldValues = OnAfterAssignFieldValues
        };
    }
}
