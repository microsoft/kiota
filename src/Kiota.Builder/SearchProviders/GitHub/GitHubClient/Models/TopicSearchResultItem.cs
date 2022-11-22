using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>Topic Search Result Item</summary>
    public class TopicSearchResultItem : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The aliases property</summary>
        public List<TopicSearchResultItem_aliases> Aliases { get; set; }
        /// <summary>The created_at property</summary>
        public DateTimeOffset? Created_at { get; set; }
        /// <summary>The created_by property</summary>
        public string Created_by { get; set; }
        /// <summary>The curated property</summary>
        public bool? Curated { get; set; }
        /// <summary>The description property</summary>
        public string Description { get; set; }
        /// <summary>The display_name property</summary>
        public string Display_name { get; set; }
        /// <summary>The featured property</summary>
        public bool? Featured { get; set; }
        /// <summary>The logo_url property</summary>
        public string Logo_url { get; set; }
        /// <summary>The name property</summary>
        public string Name { get; set; }
        /// <summary>The related property</summary>
        public List<TopicSearchResultItem_related> Related { get; set; }
        /// <summary>The released property</summary>
        public string Released { get; set; }
        /// <summary>The repository_count property</summary>
        public int? Repository_count { get; set; }
        /// <summary>The score property</summary>
        public int? Score { get; set; }
        /// <summary>The short_description property</summary>
        public string Short_description { get; set; }
        /// <summary>The text_matches property</summary>
        public List<TopicSearchResultItem_text_matches> Text_matches { get; set; }
        /// <summary>The updated_at property</summary>
        public DateTimeOffset? Updated_at { get; set; }
        /// <summary>
        /// Instantiates a new topicSearchResultItem and sets the default values.
        /// </summary>
        public TopicSearchResultItem() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static TopicSearchResultItem CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new TopicSearchResultItem();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"aliases", n => { Aliases = n.GetCollectionOfObjectValues<TopicSearchResultItem_aliases>(TopicSearchResultItem_aliases.CreateFromDiscriminatorValue)?.ToList(); } },
                {"created_at", n => { Created_at = n.GetDateTimeOffsetValue(); } },
                {"created_by", n => { Created_by = n.GetStringValue(); } },
                {"curated", n => { Curated = n.GetBoolValue(); } },
                {"description", n => { Description = n.GetStringValue(); } },
                {"display_name", n => { Display_name = n.GetStringValue(); } },
                {"featured", n => { Featured = n.GetBoolValue(); } },
                {"logo_url", n => { Logo_url = n.GetStringValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"related", n => { Related = n.GetCollectionOfObjectValues<TopicSearchResultItem_related>(TopicSearchResultItem_related.CreateFromDiscriminatorValue)?.ToList(); } },
                {"released", n => { Released = n.GetStringValue(); } },
                {"repository_count", n => { Repository_count = n.GetIntValue(); } },
                {"score", n => { Score = n.GetIntValue(); } },
                {"short_description", n => { Short_description = n.GetStringValue(); } },
                {"text_matches", n => { Text_matches = n.GetCollectionOfObjectValues<TopicSearchResultItem_text_matches>(TopicSearchResultItem_text_matches.CreateFromDiscriminatorValue)?.ToList(); } },
                {"updated_at", n => { Updated_at = n.GetDateTimeOffsetValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteCollectionOfObjectValues<TopicSearchResultItem_aliases>("aliases", Aliases);
            writer.WriteDateTimeOffsetValue("created_at", Created_at);
            writer.WriteStringValue("created_by", Created_by);
            writer.WriteBoolValue("curated", Curated);
            writer.WriteStringValue("description", Description);
            writer.WriteStringValue("display_name", Display_name);
            writer.WriteBoolValue("featured", Featured);
            writer.WriteStringValue("logo_url", Logo_url);
            writer.WriteStringValue("name", Name);
            writer.WriteCollectionOfObjectValues<TopicSearchResultItem_related>("related", Related);
            writer.WriteStringValue("released", Released);
            writer.WriteIntValue("repository_count", Repository_count);
            writer.WriteIntValue("score", Score);
            writer.WriteStringValue("short_description", Short_description);
            writer.WriteCollectionOfObjectValues<TopicSearchResultItem_text_matches>("text_matches", Text_matches);
            writer.WriteDateTimeOffsetValue("updated_at", Updated_at);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
