using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    public class TopicSearchResultItem_related : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The topic_relation property</summary>
        public TopicSearchResultItem_related_topic_relation Topic_relation { get; set; }
        /// <summary>
        /// Instantiates a new topicSearchResultItem_related and sets the default values.
        /// </summary>
        public TopicSearchResultItem_related() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static TopicSearchResultItem_related CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new TopicSearchResultItem_related();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"topic_relation", n => { Topic_relation = n.GetObjectValue<TopicSearchResultItem_related_topic_relation>(TopicSearchResultItem_related_topic_relation.CreateFromDiscriminatorValue); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteObjectValue<TopicSearchResultItem_related_topic_relation>("topic_relation", Topic_relation);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
