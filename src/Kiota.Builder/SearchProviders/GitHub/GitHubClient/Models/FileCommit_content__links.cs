using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    public class FileCommit_content__links : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The git property</summary>
        public string Git { get; set; }
        /// <summary>The html property</summary>
        public string Html { get; set; }
        /// <summary>The self property</summary>
        public string Self { get; set; }
        /// <summary>
        /// Instantiates a new FileCommit_content__links and sets the default values.
        /// </summary>
        public FileCommit_content__links() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static FileCommit_content__links CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new FileCommit_content__links();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"git", n => { Git = n.GetStringValue(); } },
                {"html", n => { Html = n.GetStringValue(); } },
                {"self", n => { Self = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("git", Git);
            writer.WriteStringValue("html", Html);
            writer.WriteStringValue("self", Self);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
