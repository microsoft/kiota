using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    public class RepoSearchResultItem_text_matches : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The fragment property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Fragment { get; set; }
#nullable restore
#else
        public string Fragment { get; set; }
#endif
        /// <summary>The matches property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public List<RepoSearchResultItem_text_matches_matches>? Matches { get; set; }
#nullable restore
#else
        public List<RepoSearchResultItem_text_matches_matches> Matches { get; set; }
#endif
        /// <summary>The object_type property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? ObjectType { get; set; }
#nullable restore
#else
        public string ObjectType { get; set; }
#endif
        /// <summary>The object_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? ObjectUrl { get; set; }
#nullable restore
#else
        public string ObjectUrl { get; set; }
#endif
        /// <summary>The property property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Property { get; set; }
#nullable restore
#else
        public string Property { get; set; }
#endif
        /// <summary>
        /// Instantiates a new repoSearchResultItem_text_matches and sets the default values.
        /// </summary>
        public RepoSearchResultItem_text_matches() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static RepoSearchResultItem_text_matches CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new RepoSearchResultItem_text_matches();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"fragment", n => { Fragment = n.GetStringValue(); } },
                {"matches", n => { Matches = n.GetCollectionOfObjectValues<RepoSearchResultItem_text_matches_matches>(RepoSearchResultItem_text_matches_matches.CreateFromDiscriminatorValue)?.ToList(); } },
                {"object_type", n => { ObjectType = n.GetStringValue(); } },
                {"object_url", n => { ObjectUrl = n.GetStringValue(); } },
                {"property", n => { Property = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("fragment", Fragment);
            writer.WriteCollectionOfObjectValues<RepoSearchResultItem_text_matches_matches>("matches", Matches);
            writer.WriteStringValue("object_type", ObjectType);
            writer.WriteStringValue("object_url", ObjectUrl);
            writer.WriteStringValue("property", Property);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
