using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>
    /// An object describing a submodule
    /// </summary>
    public class ContentSubmodule : IAdditionalDataHolder, IParsable {
        /// <summary>The _links property</summary>
        public ContentSubmodule__links _links { get; set; }
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
        public string Path { get; set; }
        /// <summary>The sha property</summary>
        public string Sha { get; set; }
        /// <summary>The size property</summary>
        public int? Size { get; set; }
        /// <summary>The submodule_git_url property</summary>
        public string Submodule_git_url { get; set; }
        /// <summary>The type property</summary>
        public ContentSubmodule_type? Type { get; set; }
        /// <summary>The url property</summary>
        public string Url { get; set; }
        /// <summary>
        /// Instantiates a new ContentSubmodule and sets the default values.
        /// </summary>
        public ContentSubmodule() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static ContentSubmodule CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new ContentSubmodule();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"_links", n => { _links = n.GetObjectValue<ContentSubmodule__links>(ContentSubmodule__links.CreateFromDiscriminatorValue); } },
                {"download_url", n => { Download_url = n.GetStringValue(); } },
                {"git_url", n => { Git_url = n.GetStringValue(); } },
                {"html_url", n => { Html_url = n.GetStringValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"path", n => { Path = n.GetStringValue(); } },
                {"sha", n => { Sha = n.GetStringValue(); } },
                {"size", n => { Size = n.GetIntValue(); } },
                {"submodule_git_url", n => { Submodule_git_url = n.GetStringValue(); } },
                {"type", n => { Type = n.GetEnumValue<ContentSubmodule_type>(); } },
                {"url", n => { Url = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer) {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteObjectValue<ContentSubmodule__links>("_links", _links);
            writer.WriteStringValue("download_url", Download_url);
            writer.WriteStringValue("git_url", Git_url);
            writer.WriteStringValue("html_url", Html_url);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("path", Path);
            writer.WriteStringValue("sha", Sha);
            writer.WriteIntValue("size", Size);
            writer.WriteStringValue("submodule_git_url", Submodule_git_url);
            writer.WriteEnumValue<ContentSubmodule_type>("type", Type);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
