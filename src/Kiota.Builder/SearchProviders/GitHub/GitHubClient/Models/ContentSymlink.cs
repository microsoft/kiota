using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models
{
    /// <summary>
    /// An object describing a symlink
    /// </summary>
    public class ContentSymlink : IAdditionalDataHolder, IParsable
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>The download_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? DownloadUrl
        {
            get; set;
        }
#nullable restore
#else
        public string DownloadUrl { get; set; }
#endif
        /// <summary>The git_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? GitUrl
        {
            get; set;
        }
#nullable restore
#else
        public string GitUrl { get; set; }
#endif
        /// <summary>The html_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? HtmlUrl
        {
            get; set;
        }
#nullable restore
#else
        public string HtmlUrl { get; set; }
#endif
        /// <summary>The _links property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public ContentSymlink__links? Links
        {
            get; set;
        }
#nullable restore
#else
        public ContentSymlink__links Links { get; set; }
#endif
        /// <summary>The name property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Name
        {
            get; set;
        }
#nullable restore
#else
        public string Name { get; set; }
#endif
        /// <summary>The path property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Path
        {
            get; set;
        }
#nullable restore
#else
        public string Path { get; set; }
#endif
        /// <summary>The sha property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Sha
        {
            get; set;
        }
#nullable restore
#else
        public string Sha { get; set; }
#endif
        /// <summary>The size property</summary>
        public int? Size
        {
            get; set;
        }
        /// <summary>The target property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Target
        {
            get; set;
        }
#nullable restore
#else
        public string Target { get; set; }
#endif
        /// <summary>The type property</summary>
        public ContentSymlink_type? Type
        {
            get; set;
        }
        /// <summary>The url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? Url
        {
            get; set;
        }
#nullable restore
#else
        public string Url { get; set; }
#endif
        /// <summary>
        /// Instantiates a new ContentSymlink and sets the default values.
        /// </summary>
        public ContentSymlink()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static ContentSymlink CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new ContentSymlink();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            return new Dictionary<string, Action<IParseNode>> {
                {"download_url", n => { DownloadUrl = n.GetStringValue(); } },
                {"git_url", n => { GitUrl = n.GetStringValue(); } },
                {"html_url", n => { HtmlUrl = n.GetStringValue(); } },
                {"_links", n => { Links = n.GetObjectValue<ContentSymlink__links>(ContentSymlink__links.CreateFromDiscriminatorValue); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"path", n => { Path = n.GetStringValue(); } },
                {"sha", n => { Sha = n.GetStringValue(); } },
                {"size", n => { Size = n.GetIntValue(); } },
                {"target", n => { Target = n.GetStringValue(); } },
                {"type", n => { Type = n.GetEnumValue<ContentSymlink_type>(); } },
                {"url", n => { Url = n.GetStringValue(); } },
            };
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteStringValue("download_url", DownloadUrl);
            writer.WriteStringValue("git_url", GitUrl);
            writer.WriteStringValue("html_url", HtmlUrl);
            writer.WriteObjectValue<ContentSymlink__links>("_links", Links);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("path", Path);
            writer.WriteStringValue("sha", Sha);
            writer.WriteIntValue("size", Size);
            writer.WriteStringValue("target", Target);
            writer.WriteEnumValue<ContentSymlink_type>("type", Type);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
