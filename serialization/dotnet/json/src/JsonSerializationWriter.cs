using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.Reflection;
using KiotaCore.Extensions;

namespace Microsoft.Kiota.Serialization.Json {
    public class JsonSerializationWriter : ISerializationWriter, IDisposable {
        private readonly MemoryStream stream = new MemoryStream();
        public readonly Utf8JsonWriter writer;
        public JsonSerializationWriter()
        {
            writer = new Utf8JsonWriter(stream);
        }
        public Stream GetSerializedContent() {
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        public void WriteStringValue(string key, string value) {
            if(value != null) { // we want to keep empty string because they are meaningfull
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                writer.WriteStringValue(value);
            }
        }
        public void WriteBoolValue(string key, bool? value) {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteBooleanValue(value.Value);
        }
        public void WriteIntValue(string key, int? value) {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteNumberValue(value.Value);
        }
        public void WriteFloatValue(string key, float? value) {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteNumberValue(value.Value);
        }
        public void WriteDoubleValue(string key, double? value) {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteNumberValue(value.Value);
        }
        public void WriteGuidValue(string key, Guid? value) {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteStringValue(value.Value);
        }
        public void WriteDateTimeOffsetValue(string key, DateTimeOffset? value) {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteStringValue(value.Value);
        }
        public void WriteEnumValue<T>(string key, T? value) where T : struct, Enum {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) {
                if(typeof(T).GetCustomAttributes<FlagsAttribute>().Any())
                    writer.WriteStringValue(Enum.GetValues<T>()
                                            .Where(x => value.Value.HasFlag(x))
                                            .Select(x => Enum.GetName<T>(x))
                                            .Select(x => x.ToFirstCharacterLowerCase())
                                            .Aggregate((x, y) => $"{x},{y}"));
                else writer.WriteStringValue(value.Value.ToString().ToFirstCharacterLowerCase());
            }
        }
        public void WriteCollectionOfPrimitiveValues<T>(string key, IEnumerable<T> values) {
            if(values != null) { //empty array is meaningful
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                writer.WriteStartArray();
                foreach(var collectionValue in values)
                    WriteAnyValue(null, collectionValue);
                writer.WriteEndArray();
            }
        }
        public void WriteCollectionOfObjectValues<T>(string key, IEnumerable<T> values) where T : class, IParsable<T>, new() {
            if(values != null) { //empty array is meaningful
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                writer.WriteStartArray();
                foreach(var item in values.Where(x => x != null)) {
                    writer.WriteStartObject();
                    item.Serialize(this);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
        }
        public void WriteObjectValue<T>(string key, T value) where T : class, IParsable<T>, new() {
            if(value != null) {
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                writer.WriteStartObject();
                value.Serialize(this);
                writer.WriteEndObject();
            }
        }
        public void WriteAdditionalData(IDictionary<string, object> value) {
            if(value == null) return;
            
            foreach(var dataValue in value)
                WriteAnyValue(dataValue.Key, dataValue.Value);
        }
        private void WriteNonParsableObjectValue<T>(string key, T value) {
            if(!string.IsNullOrEmpty(key))
                writer.WritePropertyName(key);
            writer.WriteStartObject();
            if(value == null) writer.WriteNullValue();
            else
                foreach(var oProp in value.GetType().GetProperties())
                    WriteAnyValue(oProp.Name, oProp.GetValue(value));
            writer.WriteEndObject();
        }
        private void WriteAnyValue<T>(string key, T value) {
            if(value == null) {
                if(!string.IsNullOrEmpty(key))
                    this.writer.WritePropertyName(key);
                this.writer.WriteNullValue();
            }
            switch(value) {
                case string s:
                    WriteStringValue(key, s);
                break;
                case bool b:
                    WriteBoolValue(key, b);
                break;
                case int i:
                    WriteIntValue(key, i);
                break;
                case float f:
                    WriteFloatValue(key, f);
                break;
                case double d:
                    WriteDoubleValue(key, d);
                break;
                case Guid g:
                    WriteGuidValue(key, g);
                break;
                case DateTimeOffset dto:
                    WriteDateTimeOffsetValue(key, dto);
                break;
                case IEnumerable<object> coll:
                    WriteCollectionOfPrimitiveValues(key, coll); // should we support collections of parsables here too?
                break;
                case object o:
                    WriteNonParsableObjectValue(key, o); // should we support parsables here too?
                break;
                default:
                    throw new InvalidOperationException($"error serialization additional data value with key {key}, unknown type {value?.GetType()}");
            }
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }
}
