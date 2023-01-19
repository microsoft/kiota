using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>
    /// A GitHub user.
    /// </summary>
    public class SimpleUser : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The avatar_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Avatar_url { get; set; }
#else
        public string Avatar_url { get; set; }
#endif
        /// <summary>The email property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Email { get; set; }
#else
        public string Email { get; set; }
#endif
        /// <summary>The events_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Events_url { get; set; }
#else
        public string Events_url { get; set; }
#endif
        /// <summary>The followers_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Followers_url { get; set; }
#else
        public string Followers_url { get; set; }
#endif
        /// <summary>The following_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Following_url { get; set; }
#else
        public string Following_url { get; set; }
#endif
        /// <summary>The gists_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Gists_url { get; set; }
#else
        public string Gists_url { get; set; }
#endif
        /// <summary>The gravatar_id property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Gravatar_id { get; set; }
#else
        public string Gravatar_id { get; set; }
#endif
        /// <summary>The html_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Html_url { get; set; }
#else
        public string Html_url { get; set; }
#endif
        /// <summary>The id property</summary>
        public int? Id { get; set; }
        /// <summary>The login property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Login { get; set; }
#else
        public string Login { get; set; }
#endif
        /// <summary>The name property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Name { get; set; }
#else
        public string Name { get; set; }
#endif
        /// <summary>The node_id property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Node_id { get; set; }
#else
        public string Node_id { get; set; }
#endif
        /// <summary>The organizations_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Organizations_url { get; set; }
#else
        public string Organizations_url { get; set; }
#endif
        /// <summary>The received_events_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Received_events_url { get; set; }
#else
        public string Received_events_url { get; set; }
#endif
        /// <summary>The repos_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Repos_url { get; set; }
#else
        public string Repos_url { get; set; }
#endif
        /// <summary>The site_admin property</summary>
        public bool? Site_admin { get; set; }
        /// <summary>The starred_at property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Starred_at { get; set; }
#else
        public string Starred_at { get; set; }
#endif
        /// <summary>The starred_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Starred_url { get; set; }
#else
        public string Starred_url { get; set; }
#endif
        /// <summary>The subscriptions_url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Subscriptions_url { get; set; }
#else
        public string Subscriptions_url { get; set; }
#endif
        /// <summary>The type property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Type { get; set; }
#else
        public string Type { get; set; }
#endif
        /// <summary>The url property</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
        public string? Url { get; set; }
#else
        public string Url { get; set; }
#endif
        /// <summary>
        /// Instantiates a new SimpleUser and sets the default values.
        /// </summary>
        public SimpleUser() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static SimpleUser CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new SimpleUser();
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() {
            return new Dictionary<string, Action<IParseNode>> {
                {"avatar_url", n => { Avatar_url = n.GetStringValue(); } },
                {"email", n => { Email = n.GetStringValue(); } },
                {"events_url", n => { Events_url = n.GetStringValue(); } },
                {"followers_url", n => { Followers_url = n.GetStringValue(); } },
                {"following_url", n => { Following_url = n.GetStringValue(); } },
                {"gists_url", n => { Gists_url = n.GetStringValue(); } },
                {"gravatar_id", n => { Gravatar_id = n.GetStringValue(); } },
                {"html_url", n => { Html_url = n.GetStringValue(); } },
                {"id", n => { Id = n.GetIntValue(); } },
                {"login", n => { Login = n.GetStringValue(); } },
                {"name", n => { Name = n.GetStringValue(); } },
                {"node_id", n => { Node_id = n.GetStringValue(); } },
                {"organizations_url", n => { Organizations_url = n.GetStringValue(); } },
                {"received_events_url", n => { Received_events_url = n.GetStringValue(); } },
                {"repos_url", n => { Repos_url = n.GetStringValue(); } },
                {"site_admin", n => { Site_admin = n.GetBoolValue(); } },
                {"starred_at", n => { Starred_at = n.GetStringValue(); } },
                {"starred_url", n => { Starred_url = n.GetStringValue(); } },
                {"subscriptions_url", n => { Subscriptions_url = n.GetStringValue(); } },
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
            writer.WriteStringValue("avatar_url", Avatar_url);
            writer.WriteStringValue("email", Email);
            writer.WriteStringValue("events_url", Events_url);
            writer.WriteStringValue("followers_url", Followers_url);
            writer.WriteStringValue("following_url", Following_url);
            writer.WriteStringValue("gists_url", Gists_url);
            writer.WriteStringValue("gravatar_id", Gravatar_id);
            writer.WriteStringValue("html_url", Html_url);
            writer.WriteIntValue("id", Id);
            writer.WriteStringValue("login", Login);
            writer.WriteStringValue("name", Name);
            writer.WriteStringValue("node_id", Node_id);
            writer.WriteStringValue("organizations_url", Organizations_url);
            writer.WriteStringValue("received_events_url", Received_events_url);
            writer.WriteStringValue("repos_url", Repos_url);
            writer.WriteBoolValue("site_admin", Site_admin);
            writer.WriteStringValue("starred_at", Starred_at);
            writer.WriteStringValue("starred_url", Starred_url);
            writer.WriteStringValue("subscriptions_url", Subscriptions_url);
            writer.WriteStringValue("type", Type);
            writer.WriteStringValue("url", Url);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
