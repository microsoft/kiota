using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    public class FileCommit_commit_parents : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The html_url property</summary>
        public string Html_url { get; set; }
        /// <summary>The sha property</summary>
        public string Sha { get; set; }
        /// <summary>The url property</summary>
        public string Url { get; set; }
        /// <summary>
        /// Instantiates a new FileCommit_commit_parents and sets the default values.
        /// </summary>
        public FileCommit_commit_parents() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static FileCommit_commit_parents CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new FileCommit_commit_parents();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"html_url", n => { Html_url = n.GetStringValue(); } },
                {"sha", n => { Sha = n.GetStringValue(); } },
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
            writer.WriteStringValue("sha", Sha);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
