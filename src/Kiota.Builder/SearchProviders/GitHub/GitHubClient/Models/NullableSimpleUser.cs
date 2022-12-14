using Microsoft.Kiota.Abstractions.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models {
    /// <summary>
    /// A GitHub user.
    /// </summary>
    public class NullableSimpleUser : IAdditionalDataHolder, IParsable {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData { get; set; }
        /// <summary>The avatar_url property</summary>
        public string Avatar_url { get; set; }
        /// <summary>The email property</summary>
        public string Email { get; set; }
        /// <summary>The events_url property</summary>
        public string Events_url { get; set; }
        /// <summary>The followers_url property</summary>
        public string Followers_url { get; set; }
        /// <summary>The following_url property</summary>
        public string Following_url { get; set; }
        /// <summary>The gists_url property</summary>
        public string Gists_url { get; set; }
        /// <summary>The gravatar_id property</summary>
        public string Gravatar_id { get; set; }
        /// <summary>The html_url property</summary>
        public string Html_url { get; set; }
        /// <summary>The id property</summary>
        public int? Id { get; set; }
        /// <summary>The login property</summary>
        public string Login { get; set; }
        /// <summary>The name property</summary>
        public string Name { get; set; }
        /// <summary>The node_id property</summary>
        public string Node_id { get; set; }
        /// <summary>The organizations_url property</summary>
        public string Organizations_url { get; set; }
        /// <summary>The received_events_url property</summary>
        public string Received_events_url { get; set; }
        /// <summary>The repos_url property</summary>
        public string Repos_url { get; set; }
        /// <summary>The site_admin property</summary>
        public bool? Site_admin { get; set; }
        /// <summary>The starred_at property</summary>
        public string Starred_at { get; set; }
        /// <summary>The starred_url property</summary>
        public string Starred_url { get; set; }
        /// <summary>The subscriptions_url property</summary>
        public string Subscriptions_url { get; set; }
        /// <summary>The type property</summary>
        public string Type { get; set; }
        /// <summary>The url property</summary>
        public string Url { get; set; }
        /// <summary>
        /// Instantiates a new nullableSimpleUser and sets the default values.
        /// </summary>
        public NullableSimpleUser() {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static NullableSimpleUser CreateFromDiscriminatorValue(IParseNode parseNode) {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            return new NullableSimpleUser();
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
