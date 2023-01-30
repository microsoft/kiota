using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
/// <summary>
/// Installation
/// </summary>
public class Installation : IAdditionalDataHolder, IParsable
{
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Access_tokens_url
    {
        get; set;
    }
#nullable restore
#else
    public string Access_tokens_url { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public Installations? Account
    {
        get; set;
    }
#nullable restore
#else
    public Installations Account { get; set; }
#endif
    /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
    public IDictionary<string, object> AdditionalData
    {
        get; set;
    }
    public int? App_id
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? App_slug
    {
        get; set;
    }
#nullable restore
#else
    public string App_slug { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Contact_email
    {
        get; set;
    }
#nullable restore
#else
    public string Contact_email { get; set; }
#endif
    public DateTimeOffset? Created_at
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public List<string>? Events
    {
        get; set;
    }
#nullable restore
#else
    public List<string> Events { get; set; }
#endif
    public bool? Has_multiple_single_files
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Html_url
    {
        get; set;
    }
#nullable restore
#else
    public string Html_url { get; set; }
#endif
    /// <summary>The ID of the installation.</summary>
    public int? Id
    {
        get; set;
    }
    /// <summary>The permissions granted to the user-to-server access token.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public AppPermissions? Permissions
    {
        get; set;
    }
#nullable restore
#else
    public AppPermissions Permissions { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Repositories_url
    {
        get; set;
    }
#nullable restore
#else
    public string Repositories_url { get; set; }
#endif
    /// <summary>Describe whether all repositories have been selected or there&apos;s a selection involved</summary>
    public Installation_repository_selection? Repository_selection
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Single_file_name
    {
        get; set;
    }
#nullable restore
#else
    public string Single_file_name { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public List<string>? Single_file_paths
    {
        get; set;
    }
#nullable restore
#else
    public List<string> Single_file_paths { get; set; }
#endif
    public DateTimeOffset? Suspended_at
    {
        get; set;
    }
    /// <summary>A GitHub user.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public NullableSimpleUser? Suspended_by
    {
        get; set;
    }
#nullable restore
#else
    public NullableSimpleUser Suspended_by { get; set; }
#endif
    /// <summary>The ID of the user or organization this token is being scoped to.</summary>
    public int? Target_id
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Target_type
    {
        get; set;
    }
#nullable restore
#else
    public string Target_type { get; set; }
#endif
    public DateTimeOffset? Updated_at
    {
        get; set;
    }
    /// <summary>
    /// Instantiates a new installation and sets the default values.
    /// </summary>
    public Installation()
    {
        AdditionalData = new Dictionary<string, object>();
    }
    /// <summary>
    /// Creates a new instance of the appropriate class based on discriminator value
    /// </summary>
    /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
    public static Installation CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
        return new Installation();
    }
    /// <summary>
    /// The deserialization information for the current model
    /// </summary>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>> {
            {"access_tokens_url", n => { Access_tokens_url = n.GetStringValue(); } },
            {"account", n => { Account = n.GetObjectValue<Installations>(Installations.CreateFromDiscriminatorValue); } },
            {"app_id", n => { App_id = n.GetIntValue(); } },
            {"app_slug", n => { App_slug = n.GetStringValue(); } },
            {"contact_email", n => { Contact_email = n.GetStringValue(); } },
            {"created_at", n => { Created_at = n.GetDateTimeOffsetValue(); } },
            {"events", n => { Events = n.GetCollectionOfPrimitiveValues<string>()?.ToList(); } },
            {"has_multiple_single_files", n => { Has_multiple_single_files = n.GetBoolValue(); } },
            {"html_url", n => { Html_url = n.GetStringValue(); } },
            {"id", n => { Id = n.GetIntValue(); } },
            {"permissions", n => { Permissions = n.GetObjectValue<AppPermissions>(AppPermissions.CreateFromDiscriminatorValue); } },
            {"repositories_url", n => { Repositories_url = n.GetStringValue(); } },
            {"repository_selection", n => { Repository_selection = n.GetEnumValue<Installation_repository_selection>(); } },
            {"single_file_name", n => { Single_file_name = n.GetStringValue(); } },
            {"single_file_paths", n => { Single_file_paths = n.GetCollectionOfPrimitiveValues<string>()?.ToList(); } },
            {"suspended_at", n => { Suspended_at = n.GetDateTimeOffsetValue(); } },
            {"suspended_by", n => { Suspended_by = n.GetObjectValue<NullableSimpleUser>(NullableSimpleUser.CreateFromDiscriminatorValue); } },
            {"target_id", n => { Target_id = n.GetIntValue(); } },
            {"target_type", n => { Target_type = n.GetStringValue(); } },
            {"updated_at", n => { Updated_at = n.GetDateTimeOffsetValue(); } },
        };
    }
    /// <summary>
    /// Serializes information the current object
    /// </summary>
    /// <param name="writer">Serialization writer to use to serialize this model</param>
    public void Serialize(ISerializationWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteStringValue("access_tokens_url", Access_tokens_url);
        writer.WriteObjectValue<Installations>("account", Account);
        writer.WriteIntValue("app_id", App_id);
        writer.WriteStringValue("app_slug", App_slug);
        writer.WriteStringValue("contact_email", Contact_email);
        writer.WriteDateTimeOffsetValue("created_at", Created_at);
        writer.WriteCollectionOfPrimitiveValues<string>("events", Events);
        writer.WriteBoolValue("has_multiple_single_files", Has_multiple_single_files);
        writer.WriteStringValue("html_url", Html_url);
        writer.WriteIntValue("id", Id);
        writer.WriteObjectValue<AppPermissions>("permissions", Permissions);
        writer.WriteStringValue("repositories_url", Repositories_url);
        writer.WriteEnumValue<Installation_repository_selection>("repository_selection", Repository_selection);
        writer.WriteStringValue("single_file_name", Single_file_name);
        writer.WriteCollectionOfPrimitiveValues<string>("single_file_paths", Single_file_paths);
        writer.WriteDateTimeOffsetValue("suspended_at", Suspended_at);
        writer.WriteObjectValue<NullableSimpleUser>("suspended_by", Suspended_by);
        writer.WriteIntValue("target_id", Target_id);
        writer.WriteStringValue("target_type", Target_type);
        writer.WriteDateTimeOffsetValue("updated_at", Updated_at);
        writer.WriteAdditionalData(AdditionalData);
    }
    /// <summary>
    /// Composed type wrapper for classes simpleUser, enterprise
    /// </summary>
    public class Installations : IAdditionalDataHolder, IParsable
    {
        /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
        public IDictionary<string, object> AdditionalData
        {
            get; set;
        }
        /// <summary>Composed type representation for type enterprise</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.Enterprise? Enterprise
        {
            get; set;
        }
#nullable restore
#else
        public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.Enterprise Enterprise { get; set; }
#endif
        /// <summary>Serialization hint for the current wrapper.</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? SerializationHint
        {
            get; set;
        }
#nullable restore
#else
        public string SerializationHint { get; set; }
#endif
        /// <summary>Composed type representation for type simpleUser</summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.SimpleUser? SimpleUser
        {
            get; set;
        }
#nullable restore
#else
        public Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.SimpleUser SimpleUser { get; set; }
#endif
        /// <summary>
        /// Instantiates a new installations and sets the default values.
        /// </summary>
        public Installations()
        {
            AdditionalData = new Dictionary<string, object>();
        }
        /// <summary>
        /// Creates a new instance of the appropriate class based on discriminator value
        /// </summary>
        /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
        public static Installations CreateFromDiscriminatorValue(IParseNode parseNode)
        {
            _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
            var result = new Installations();
            result.Enterprise = new Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.Enterprise();
            result.SimpleUser = new Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.SimpleUser();
            return result;
        }
        /// <summary>
        /// The deserialization information for the current model
        /// </summary>
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
        {
            if (Enterprise != null || SimpleUser != null)
            {
                return ParseNodeHelper.MergeDeserializersForIntersectionWrapper(Enterprise, SimpleUser);
            }
            return new Dictionary<string, Action<IParseNode>>();
        }
        /// <summary>
        /// Serializes information the current object
        /// </summary>
        /// <param name="writer">Serialization writer to use to serialize this model</param>
        public void Serialize(ISerializationWriter writer)
        {
            _ = writer ?? throw new ArgumentNullException(nameof(writer));
            writer.WriteObjectValue<Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models.Enterprise>(null, Enterprise, SimpleUser);
            writer.WriteAdditionalData(AdditionalData);
        }
    }
}
