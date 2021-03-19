using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Kiota.Abstractions.Serialization;

namespace KiotaCore.Serialization {
    public class JsonParseNode : IParseNode {
        private readonly JsonElement _jsonNode;
        public JsonParseNode(JsonElement node)
        {
            _jsonNode = node;
        }
        public string GetStringValue() => _jsonNode.GetString();
        public bool GetBoolValue() => _jsonNode.GetBoolean();
        public int GetIntValue() => _jsonNode.GetInt32();
        public decimal GetFloatValue() => _jsonNode.GetDecimal();
        public double GetDoubleValue() => _jsonNode.GetDouble();
        public Guid GetGuidValue() => _jsonNode.GetGuid();
        public DateTimeOffset GetDateTimeOffsetValue() => _jsonNode.GetDateTimeOffset();
        public IEnumerable<T> GetCollectionOfObjectValues<T>() where T: class, IParsable<T>, new() {
            var enumerator = _jsonNode.EnumerateArray();
            while(enumerator.MoveNext()) {
                var currentParseNode = new JsonParseNode(enumerator.Current);
                yield return currentParseNode.GetObjectValue<T>();
            }
        }
        public IEnumerable<T> GetCollectionOfPrimitiveValues<T>() {
            var enumerator = _jsonNode.EnumerateArray();
            while(enumerator.MoveNext()) {
                var currentParseNode = new JsonParseNode(enumerator.Current);
                var genericType = typeof(T);
                if(genericType == typeof(bool))
                    yield return (T)(object)currentParseNode.GetBoolValue();
                else if(genericType == typeof(string))
                    yield return (T)(object)currentParseNode.GetStringValue();
                else if(genericType == typeof(int))
                    yield return (T)(object)currentParseNode.GetIntValue();
                else if(genericType == typeof(float))
                    yield return (T)(object)currentParseNode.GetFloatValue();
                else if(genericType == typeof(double))
                    yield return (T)(object)currentParseNode.GetDoubleValue();
                else if(genericType == typeof(Guid))
                    yield return (T)(object)currentParseNode.GetGuidValue();
                else if(genericType == typeof(DateTimeOffset))
                    yield return (T)(object)currentParseNode.GetGuidValue();
                else
                    throw new InvalidOperationException($"unknown type for deserialization {genericType.FullName}");
            }
        }
        private static Type objectType = typeof(object);
        public T GetObjectValue<T>() where T: class, IParsable<T>, new() {
            var item = new T();
            var baseType = typeof(T).BaseType;
            while(baseType != null && baseType != objectType) {
                Debug.WriteLine($"setting property values for parent type {baseType.Name}");
                var baseTypeFieldsProperty = baseType.GetProperty(nameof(item.DeserializeFields), BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                if(baseTypeFieldsProperty == null)
                    baseType = null;
                else {
                    var baseTypeFields = (IEnumerable)baseTypeFieldsProperty.GetValue(item);
                    AssignFieldValues(item, baseTypeFields);
                    baseType = baseType.BaseType;
                }
            }
            AssignFieldValues(item, item.DeserializeFields);
            return item;
        }
        private void AssignFieldValues<T>(T item, IEnumerable fieldDeserializers) where T: class, IParsable<T>, new() { 
            foreach(var fieldDeserializer in fieldDeserializers) {
                // we need that esotheric reflection + casting combination because covariance is not supported when trying to cast to IDictionary<string, Action<T or object, IParseNode>> above
                var objectType = fieldDeserializer.GetType();
                var key = objectType.GetProperty("Key").GetValue(fieldDeserializer) as string;
                Debug.WriteLine($"getting property {key}");
                try {
                    var fieldValue = _jsonNode.GetProperty(key);
                    if(fieldValue.ValueKind != JsonValueKind.Null) {
                        var action = objectType.GetProperty("Value").GetValue(fieldDeserializer) as Action<T, IParseNode>;
                        action.Invoke(item, new JsonParseNode(fieldValue));
                    }
                } catch(KeyNotFoundException) {
                    Debug.WriteLine($"couldn't find property {key}");
                }
            }
        }
        public IParseNode GetChildNode(string identifier) => new JsonParseNode(_jsonNode.GetProperty(identifier ?? throw new ArgumentNullException(nameof(identifier))));
    }
}
