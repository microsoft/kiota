using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>License Simple</summary>
    public class LicenseSimple : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The html_url property</summary>
        public string Html_url { get; set; }
        /// <summary>The key property</summary>
        public string Key { get; set; }
        /// <summary>The name property</summary>
        public string Name { get; set; }
        /// <summary>The node_id property</summary>
        public string Node_id { get; set; }
        /// <summary>The spdx_id property</summary>
        public string Spdx_id { get; set; }
        /// <summary>The url property</summary>
        public string Url { get; set; }
        /// <summary>
        /// Instantiates a new LicenseSimple and sets the default values.
        /// </summary>
        public LicenseSimple() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static LicenseSimple CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new LicenseSimple();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"html_url", n => { Html_url = n.GetStringValue(); } },
                {"key", n => { Key = n.GetStringValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"node_id", n => { Node_id = n.GetStringValue(); } },
                {"spdx_id", n => { Spdx_id = n.GetStringValue(); } },
                {"url", n => { Url = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("html_url", Html_url);
            writer.WriteStringValue("key", Key);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("node_id", Node_id);
            writer.WriteStringValue("spdx_id", Spdx_id);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
