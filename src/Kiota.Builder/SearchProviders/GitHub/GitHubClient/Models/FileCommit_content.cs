using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    public class FileCommit_content : IAdditionalDataHolder, IParsable {
        /// <summary>The _links property</summary>
        public FileCommit_content__links _links { get; set; }
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The download_url property</summary>
        public string Download_url { get; set; }
        /// <summary>The git_url property</summary>
        public string Git_url { get; set; }
        /// <summary>The html_url property</summary>
        public string Html_url { get; set; }
        /// <summary>The name property</summary>
        public string Name { get; set; }
        /// <summary>The path property</summary>
        public string PathObject { get; set; }
        /// <summary>The sha property</summary>
        public string Sha { get; set; }
        /// <summary>The size property</summary>
        public int? Size { get; set; }
        /// <summary>The type property</summary>
        public string Type { get; set; }
        /// <summary>The url property</summary>
        public string Url { get; set; }
        /// <summary>
        /// Instantiates a new FileCommit_content and sets the default values.
        /// </summary>
        public FileCommit_content() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static FileCommit_content CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new FileCommit_content();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"_links", n => { _links = n.GetObjectValue<FileCommit_content__links>(FileCommit_content__links.CreateFromDiscriminatorValue); } },
                {"download_url", n => { Download_url = n.GetStringValue(); } },
                {"git_url", n => { Git_url = n.GetStringValue(); } },
                {"html_url", n => { Html_url = n.GetStringValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"path", n => { PathObject = n.GetStringValue(); } },
                {"sha", n => { Sha = n.GetStringValue(); } },
                {"size", n => { Size = n.GetIntValue(); } },
                {"type", n => { Type = n.GetStringValue(); } },
                {"url", n => { Url = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteObjectValue<FileCommit_content__links>("_links", _links);
            writer.WriteStringValue("download_url", Download_url);
            writer.WriteStringValue("git_url", Git_url);
            writer.WriteStringValue("html_url", Html_url);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("path", PathObject);
            writer.WriteStringValue("sha", Sha);
            writer.WriteIntValue("size", Size);
            writer.WriteStringValue("type", Type);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
