using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>
    /// License Simple
    /// </summary>
    public class NullableLicenseSimple : IAdditionalDataHolder, IParsable
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>The html_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? HtmlUrl
        {
            get; set;
        }
#nullable restore
#else
        public string HtmlUrl { get; set; }
#endif
        /// <summary>The key property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Key
        {
            get; set;
        }
#nullable restore
#else
        public string Key { get; set; }
#endif
        /// <summary>The name property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Name
        {
            get; set;
        }
#nullable restore
#else
        public string Name { get; set; }
#endif
        /// <summary>The node_id property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? NodeId
        {
            get; set;
        }
#nullable restore
#else
        public string NodeId { get; set; }
#endif
        /// <summary>The spdx_id property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? SpdxId
        {
            get; set;
        }
#nullable restore
#else
        public string SpdxId { get; set; }
#endif
        /// <summary>The url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Url
        {
            get; set;
        }
#nullable restore
#else
        public string Url { get; set; }
#endif
        /// <summary>
        /// Instantiates a new nullableLicenseSimple and sets the default values.
        /// </summary>
        public NullableLicenseSimple()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static NullableLicenseSimple CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new NullableLicenseSimple();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"html_url", n => { HtmlUrl = n.GetStringValue(); } },
                {"key", n => { Key = n.GetStringValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"node_id", n => { NodeId = n.GetStringValue(); } },
                {"spdx_id", n => { SpdxId = n.GetStringValue(); } },
                {"url", n => { Url = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("html_url", HtmlUrl);
            writer.WriteStringValue("key", Key);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("node_id", NodeId);
            writer.WriteStringValue("spdx_id", SpdxId);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
