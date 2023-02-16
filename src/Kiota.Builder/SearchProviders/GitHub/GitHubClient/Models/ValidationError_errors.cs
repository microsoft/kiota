using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    public class ValidationError_errors : IAdditionalDataHolder, IParsable
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>The code property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Code
        {
            get; set;
        }
#nullable restore
#else
        public string Code { get; set; }
#endif
        /// <summary>The field property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Field
        {
            get; set;
        }
#nullable restore
#else
        public string Field { get; set; }
#endif
        /// <summary>The index property</summary>
        public int? Index
        {
            get; set;
        }
        /// <summary>The message property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Message
        {
            get; set;
        }
#nullable restore
#else
        public string Message { get; set; }
#endif
        /// <summary>The resource property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Resource
        {
            get; set;
        }
#nullable restore
#else
        public string Resource { get; set; }
#endif
        /// <summary>The value property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public Repositories? Value
        {
            get; set;
        }
#nullable restore
#else
        public Repositories Value { get; set; }
#endif
        /// <summary>
        /// Instantiates a new ValidationError_errors and sets the default values.
        /// </summary>
        public ValidationError_errors()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static ValidationError_errors CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new ValidationError_errors();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"code", n => { Code = n.GetStringValue(); } },
                {"field", n => { Field = n.GetStringValue(); } },
                {"index", n => { Index = n.GetIntValue(); } },
                {"message", n => { Message = n.GetStringValue(); } },
                {"resource", n => { Resource = n.GetStringValue(); } },
                {"value", n => { Value = n.GetObjectValue<Repositories>(Repositories.CreateFromDiscriminatorValue); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("code", Code);
            writer.WriteStringValue("field", Field);
            writer.WriteIntValue("index", Index);
            writer.WriteStringValue("message", Message);
            writer.WriteStringValue("resource", Resource);
            writer.WriteObjectValue<Repositories>("value", Value);
            writer.WriteAdditionalData(AdditionalData);
        }
        /// <summary>
        /// Composed type wrapper for classes string, integer, string
        /// </summary>
        public class Repositories : IAdditionalDataHolder, IParsable
        {
            /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
            public IDictionary<string, object> AdditionalData
            {
                get; set;
            }
            /// <summary>Composed type representation for type integer</summary>
            public int? Integer
            {
                get; set;
            }
            /// <summary>Serialization hint for the current wrapper.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
            public string? SerializationHint
            {
                get; set;
            }
#nullable restore
#else
            public string SerializationHint { get; set; }
#endif
            /// <summary>Composed type representation for type string</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
            public string? String
            {
                get; set;
            }
#nullable restore
#else
            public string String { get; set; }
#endif
            /// <summary>
            /// Instantiates a new repositories and sets the default values.
            /// </summary>
            public Repositories()
            {
                AdditionalData = new Dictionary<string, object>();
            }
            /// <summary>
            /// Creates a new instance of the appropriate class based on discriminator value
            /// </summary>
            /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
            public static Repositories CreateFromDiscriminatorValue(IParseNode parseNode)
            {
                _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
                var mappingValue = parseNode.GetChildNode("")?.GetStringValue();
                var result = new Repositories();
                if (parseNode.GetIntValue() is int integerValue)
                {
                    result.Integer = integerValue;
                }
                else if (parseNode.GetStringValue() is string stringValue)
                {
                    result.String = stringValue;
                }
                return result;
            }
            /// <summary>
            /// The deserialization information for the current model
            /// </summary>
            public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
            {
                return new Dictionary<string, Action<IParseNode>>();
            }
            /// <summary>
            /// Serializes information the current object
            /// </summary>
            /// <param name="writer">Serialization writer to use to serialize this model</param>
            public void Serialize(ISerializationWriter writer)
            {
                _ = writer ?? throw new ArgumentNullException(nameof(writer));
                if (Integer != null)
                {
                    writer.WriteIntValue(null, Integer);
                }
                else if (String != null)
                {
                    writer.WriteStringValue(null, String);
                }
                writer.WriteAdditionalData(AdditionalData);
            }
        }
    }
}
