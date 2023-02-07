using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    public class Repositories503Error : ApiException, IAdditionalDataHolder, IParsable
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
        /// <summary>The documentation_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? DocumentationUrl
        {
            get; set;
        }
#nullable restore
#else
        public string DocumentationUrl { get; set; }
#endif
        /// <summary>The message property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? MessageEscaped
        {
            get; set;
        }
#nullable restore
#else
        public string MessageEscaped { get; set; }
#endif
        /// <summary>
        /// Instantiates a new repositories503Error and sets the default values.
        /// </summary>
        public Repositories503Error()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static Repositories503Error CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new Repositories503Error();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"code", n => { Code = n.GetStringValue(); } },
                {"documentation_url", n => { DocumentationUrl = n.GetStringValue(); } },
                {"message", n => { MessageEscaped = n.GetStringValue(); } },
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
            writer.WriteStringValue("documentation_url", DocumentationUrl);
            writer.WriteStringValue("message", MessageEscaped);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
