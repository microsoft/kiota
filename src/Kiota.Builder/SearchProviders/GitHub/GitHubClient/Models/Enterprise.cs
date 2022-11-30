using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>An enterprise on GitHub.</summary>
    public class Enterprise : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The avatar_url property</summary>
        public string Avatar_url { get; set; }
        /// <summary>The created_at property</summary>
        public DateTimeOffset? Created_at { get; set; }
        /// <summary>A short description of the enterprise.</summary>
        public string Description { get; set; }
        /// <summary>The html_url property</summary>
        public string Html_url { get; set; }
        /// <summary>Unique identifier of the enterprise</summary>
        public int? Id { get; set; }
        /// <summary>The name of the enterprise.</summary>
        public string Name { get; set; }
        /// <summary>The node_id property</summary>
        public string Node_id { get; set; }
        /// <summary>The slug url identifier for the enterprise.</summary>
        public string Slug { get; set; }
        /// <summary>The updated_at property</summary>
        public DateTimeOffset? Updated_at { get; set; }
        /// <summary>The enterprise&apos;s website URL.</summary>
        public string Website_url { get; set; }
        /// <summary>
        /// Instantiates a new Enterprise and sets the default values.
        /// </summary>
        public Enterprise() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static Enterprise CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new Enterprise();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"avatar_url", n => { Avatar_url = n.GetStringValue(); } },
                {"created_at", n => { Created_at = n.GetDateTimeOffsetValue(); } },
                {"description", n => { Description = n.GetStringValue(); } },
                {"html_url", n => { Html_url = n.GetStringValue(); } },
                {"id", n => { Id = n.GetIntValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"node_id", n => { Node_id = n.GetStringValue(); } },
                {"slug", n => { Slug = n.GetStringValue(); } },
                {"updated_at", n => { Updated_at = n.GetDateTimeOffsetValue(); } },
                {"website_url", n => { Website_url = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("avatar_url", Avatar_url);
            writer.WriteDateTimeOffsetValue("created_at", Created_at);
            writer.WriteStringValue("description", Description);
            writer.WriteStringValue("html_url", Html_url);
            writer.WriteIntValue("id", Id);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("node_id", Node_id);
            writer.WriteStringValue("slug", Slug);
            writer.WriteDateTimeOffsetValue("updated_at", Updated_at);
            writer.WriteStringValue("website_url", Website_url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
