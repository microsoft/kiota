using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Kiota.Abstractions.Serialization {
    /// <summary>
    /// Defines an interface for serialization of objects to a stream.
    /// </summary>
    public interface ISerializationWriter : IDisposable {
        /// <summary>
        /// Writes the specified string value to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The string value to be written.</param>
        void WriteStringValue(string key, string value);
        /// <summary>
        /// Writes the specified byte boolean value to the stream with an optional given key. 
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The byte boolean value to be written.</param>
        void WriteBoolValue(string key, bool? value);
        /// <summary>
        /// Writes the specified byte integer value to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The byte integer value to be written.</param>
        void WriteIntValue(string key, int? value);
        /// <summary>
        /// Writes the specified float value to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The float value to be written.</param>
        void WriteFloatValue(string key, float? value);
        /// <summary>
        /// Writes the specified double value to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The double value to be written.</param>
        void WriteDoubleValue(string key, double? value);
        /// <summary>
        /// Writes the specified Guid value to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The Guid value to be written.</param>
        void WriteGuidValue(string key, Guid? value);
        /// <summary>
        /// Writes the specified DateTimeOffset value to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The DateTimeOffset value to be written.</param>
        void WriteDateTimeOffsetValue(string key, DateTimeOffset? value);
        /// <summary>
        /// Writes the specified collection of primitive values to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="values">The collection of primitive values to be written.</param>
        void WriteCollectionOfPrimitiveValues<T>(string key, IEnumerable<T> values);
        /// <summary>
        /// Writes the specified collection of model objects to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="values">The collection of model objects to be written.</param>
        void WriteCollectionOfObjectValues<T>(string key, IEnumerable<T> values) where T : IParsable;
        /// <summary>
        /// Writes the specified model object to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The model object to be written.</param>
        void WriteObjectValue<T>(string key, T value) where T : IParsable;
        /// <summary>
        /// Writes the specified enum value to the stream with an optional given key.
        /// </summary>
        /// <param name="key">The key to be used for the written value. May be null.</param>
        /// <param name="value">The enum value to be written.</param>
        void WriteEnumValue<T>(string key, T? value) where T : struct, Enum;
        /// <summary>
        /// Writes the specified additional data to the stream.
        /// </summary>
        /// <param name="data">The additional data to be written.</param>
        void WriteAdditionalData(IDictionary<string, object> value);
        /// <summary>
        /// Gets the value of the serialized content.
        /// </summary>
        /// <returns>The value of the serialized content.</returns>
        Stream GetSerializedContent();
        /// <summary>
        /// Callback called before the serialization process starts.
        /// </summary>
        Action<IParsable> OnBeforeObjectSerialization { get; set; }
        /// <summary>
        /// Callback called after the serialization process ends.
        /// </summary>
        Action<IParsable> OnAfterObjectSerialization { get; set; }
    }
}
