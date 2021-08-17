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
        /// <param name="node"></param>
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
        public decimal? GetFloatValue() => _jsonNode.GetDecimal();

        /// <summary>
        /// Get the double value from the json node
        /// </summary>
        /// <returns>A double value</returns>
        public double? GetDoubleValue() => _jsonNode.GetDouble();

        /// <summary>
        /// Get the guid value from the json node
        /// </summary>
        /// <returns>A guid value</returns>
        public Guid? GetGuidValue() => _jsonNode.GetGuid();

        /// <summary>
        /// Get the <see cref="DateTimeOffset"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="DateTimeOffset"/> value</returns>
        public DateTimeOffset? GetDateTimeOffsetValue() => _jsonNode.GetDateTimeOffset();

        /// <summary>
        /// Get the enumeration value of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <returns>An enumeration value or null</returns>
        public T? GetEnumValue<T>() where T : struct, Enum
        {
            var rawValue = _jsonNode.GetString();
            if(string.IsNullOrEmpty(rawValue)) return default;
            if(typeof(T).GetCustomAttributes<FlagsAttribute>().Any())
            {
                return (T)(object)rawValue
                    .Split(',')
                    .Select(x => Enum.Parse<T>(x, true))
                    .Select(x => (int)(object)x)
                    .Sum();
            }
            else
                return Enum.Parse<T>(rawValue, true);
        }

        /// <summary>
        /// Get the collection of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <returns>A collection of objects</returns>
        public IEnumerable<T> GetCollectionOfObjectValues<T>() where T : IParsable
        {
            var enumerator = _jsonNode.EnumerateArray();
            while(enumerator.MoveNext())
            {
                var currentParseNode = new JsonParseNode(enumerator.Current)
                {
                    OnAfterAssignFieldValues = OnAfterAssignFieldValues,
                    OnBeforeAssignFieldValues = OnBeforeAssignFieldValues
                };
                yield return currentParseNode.GetObjectValue<T>();
            }
        }
        private static Type booleanType = typeof(bool?);
        private static Type stringType = typeof(string);
        private static Type intType = typeof(int?);
        private static Type floatType = typeof(float?);
        private static Type doubleType = typeof(double?);
        private static Type guidType = typeof(Guid?);
        private static Type dateTimeOffsetType = typeof(DateTimeOffset?);

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
        private static Type objectType = typeof(object);

        /// <summary>
        /// Get the object of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <returns>A object of the specified type</returns>
        public T GetObjectValue<T>() where T : IParsable
        {
            var item = (T)(typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { }));
            var fieldDeserializers = item.GetFieldDeserializers<T>();
            OnBeforeAssignFieldValues?.Invoke(item);
            AssignFieldValues(item, fieldDeserializers);
            OnAfterAssignFieldValues?.Invoke(item);
            return item;
        }
        private void AssignFieldValues<T>(T item, IDictionary<string, Action<T, IParseNode>> fieldDeserializers) where T : IParsable
        {
            if(_jsonNode.ValueKind != JsonValueKind.Object) return;
            if(item.AdditionalData == null)
                item.AdditionalData = new Dictionary<string, object>();

            foreach(var fieldValue in _jsonNode.EnumerateObject().Where(x => x.Value.ValueKind != JsonValueKind.Null))
            {
                if(fieldDeserializers.ContainsKey(fieldValue.Name))
                {
                    var fieldDeserializer = fieldDeserializers[fieldValue.Name];
                    Debug.WriteLine($"found property {fieldValue.Name} to deserialize");
                    fieldDeserializer.Invoke(item, new JsonParseNode(fieldValue.Value)
                    {
                        OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
                        OnAfterAssignFieldValues = OnAfterAssignFieldValues
                    });
                }
                else
                {
                    Debug.WriteLine($"found additional property {fieldValue.Name} to deserialize");
                    item.AdditionalData.TryAdd(fieldValue.Name, TryGetAnything(fieldValue.Value));
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
