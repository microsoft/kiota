using Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Search.Repositories {
    public class RepositoriesResponse : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The incomplete_results property</summary>
        public bool? Incomplete_results { get; set; }
        /// <summary>The items property</summary>
        public List<RepoSearchResultItem> Items { get; set; }
        /// <summary>The total_count property</summary>
        public int? Total_count { get; set; }
        /// <summary>
        /// Instantiates a new repositoriesResponse and sets the default values.
        /// </summary>
        public RepositoriesResponse() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static RepositoriesResponse CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new RepositoriesResponse();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"incomplete_results", n => { Incomplete_results = n.GetBoolValue(); } },
                {"items", n => { Items = n.GetCollectionOfObjectValues<RepoSearchResultItem>(RepoSearchResultItem.CreateFromDiscriminatorValue)?.ToList(); } },
                {"total_count", n => { Total_count = n.GetIntValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteBoolValue("incomplete_results", Incomplete_results);
            writer.WriteCollectionOfObjectValues<RepoSearchResultItem>("items", Items);
            writer.WriteIntValue("total_count", Total_count);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
