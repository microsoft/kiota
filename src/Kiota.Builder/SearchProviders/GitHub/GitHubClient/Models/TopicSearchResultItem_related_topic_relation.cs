using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    public class TopicSearchResultItem_related_topic_relation : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The id property</summary>
        public int? Id { get; set; }
        /// <summary>The name property</summary>
        public string Name { get; set; }
        /// <summary>The relation_type property</summary>
        public string Relation_type { get; set; }
        /// <summary>The topic_id property</summary>
        public int? Topic_id { get; set; }
        /// <summary>
        /// Instantiates a new topicSearchResultItem_related_topic_relation and sets the default values.
        /// </summary>
        public TopicSearchResultItem_related_topic_relation() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static TopicSearchResultItem_related_topic_relation CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new TopicSearchResultItem_related_topic_relation();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"id", n => { Id = n.GetIntValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"relation_type", n => { Relation_type = n.GetStringValue(); } },
                {"topic_id", n => { Topic_id = n.GetIntValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteIntValue("id", Id);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("relation_type", Relation_type);
            writer.WriteIntValue("topic_id", Topic_id);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
