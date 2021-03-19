using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static Type booleanType = typeof(bool);
        private static Type stringType = typeof(string);
        private static Type intType = typeof(int);
        private static Type floatType = typeof(float);
        private static Type doubleType = typeof(double);
        private static Type guidType = typeof(Guid);
        private static Type dateTimeOffsetType = typeof(DateTimeOffset);
        public IEnumerable<T> GetCollectionOfPrimitiveValues<T>() {
            foreach(var collectionValue in _jsonNode.EnumerateArray()) {
                var currentParseNode = new JsonParseNode(collectionValue);
                var genericType = typeof(T);
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
                    yield return (T)(object)currentParseNode.GetGuidValue();
                else
                    throw new InvalidOperationException($"unknown type for deserialization {genericType.FullName}");
            }
        }
        private static Type objectType = typeof(object);
        public T GetObjectValue<T>() where T: class, IParsable<T>, new() {
            var item = new T();
            var fieldDeserializers = GetFieldDeserializers(item);
            AssignFieldValues(item, fieldDeserializers);
            //TODO additional properties that didn't fit into fields
            return item;
        }
        private Dictionary<string, Action<T, IParseNode>> GetFieldDeserializers<T>(T item) where T: class, IParsable<T>, new() {
            //note: we might be able to save a lot of cycles by simply "caching" these dictionaries with their types in a static property
            var baseType = typeof(T).BaseType;
            var fieldDeserializers = new Dictionary<string, Action<T, IParseNode>>(item.DeserializeFields);
            while(baseType != null && baseType != objectType) {
                Debug.WriteLine($"setting property values for parent type {baseType.Name}");
                var baseTypeFieldsProperty = baseType.GetProperty(nameof(item.DeserializeFields), BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                if(baseTypeFieldsProperty == null)
                    baseType = null;
                else {
                    var baseTypeFieldDeserializers = baseTypeFieldsProperty.GetValue(item) as IEnumerable;
                    // cannot be cast to IDictionary<string, Action<T, IParseNode>> as action generic types are contra variant
                    Type baseFieldDeserializerType  = null;
                    PropertyInfo keyProperty = null;
                    PropertyInfo valuePropery = null;
                    foreach(var baseTypeFieldDeserializer in baseTypeFieldDeserializers) {
                        // cheap lazy loading to avoid running reflection on every object of the collection when we know they are the same type
                        if(baseFieldDeserializerType == null) baseFieldDeserializerType = baseTypeFieldDeserializer.GetType();
                        if(keyProperty == null) keyProperty = baseFieldDeserializerType.GetProperty("Key");
                        if(valuePropery == null) valuePropery = baseFieldDeserializerType.GetProperty("Value");

                        var key = keyProperty.GetValue(baseTypeFieldDeserializer) as string;
                        var action = valuePropery.GetValue(baseTypeFieldDeserializer) as Action<T, IParseNode>;
                        fieldDeserializers.Add(key, action);
                    }
                    baseType = baseType.BaseType;
                }
            }
            return fieldDeserializers;
        }
        private void AssignFieldValues<T>(T item, Dictionary<string, Action<T, IParseNode>> fieldDeserializers) where T: class, IParsable<T>, new() {
            if(_jsonNode.ValueKind == JsonValueKind.Object)
                foreach(var fieldValue in _jsonNode.EnumerateObject()) {
                    if(fieldValue.Value.ValueKind != JsonValueKind.Null && fieldDeserializers.ContainsKey(fieldValue.Name)) {
                        var fieldDeserializer = fieldDeserializers[fieldValue.Name];
                        Debug.WriteLine($"found property {fieldValue.Name} to deserialize");
                        fieldDeserializer.Invoke(item, new JsonParseNode(fieldValue.Value));
                    }
                }
        }
        public IParseNode GetChildNode(string identifier) => new JsonParseNode(_jsonNode.GetProperty(identifier ?? throw new ArgumentNullException(nameof(identifier))));
    }
}
