﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Kiota.Abstractions.Serialization;
namespace Kiota.Builder.SearchProviders.GitHub.GitHubClient.Models;
public class FileCommit_content : IAdditionalDataHolder, IParsable
{
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public FileCommit_content__links? _links
    {
        get; set;
    }
#nullable restore
#else
    public FileCommit_content__links _links { get; set; }
#endif
    /// <summary>Stores additional data not described in the OpenAPI description found when deserializing. Can be used for serialization as well.</summary>
    public IDictionary<string, object> AdditionalData
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Download_url
    {
        get; set;
    }
#nullable restore
#else
    public string Download_url { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Git_url
    {
        get; set;
    }
#nullable restore
#else
    public string Git_url { get; set; }
#endif
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
    public int? Size
    {
        get; set;
    }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
    public string? Type
    {
        get; set;
    }
#nullable restore
#else
    public string Type { get; set; }
#endif
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
    /// Instantiates a new FileCommit_content and sets the default values.
    /// </summary>
    public FileCommit_content()
    {
        AdditionalData = new Dictionary<string, object>();
    }
    /// <summary>
    /// Creates a new instance of the appropriate class based on discriminator value
    /// </summary>
    /// <param name="parseNode">The parse node to use to read the discriminator value and create the object</param>
    public static FileCommit_content CreateFromDiscriminatorValue(IParseNode parseNode)
    {
        _ = parseNode ?? throw new ArgumentNullException(nameof(parseNode));
        return new FileCommit_content();
    }
    /// <summary>
    /// The deserialization information for the current model
    /// </summary>
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>> {
            {"_links", n => { _links = n.GetObjectValue<FileCommit_content__links>(FileCommit_content__links.CreateFromDiscriminatorValue); } },
            {"download_url", n => { Download_url = n.GetStringValue(); } },
            {"git_url", n => { Git_url = n.GetStringValue(); } },
            {"html_url", n => { Html_url = n.GetStringValue(); } },
            {"name", n => { Name = n.GetStringValue(); } },
            {"path", n => { Path = n.GetStringValue(); } },
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
    public void Serialize(ISerializationWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteObjectValue<FileCommit_content__links>("_links", _links);
        writer.WriteStringValue("download_url", Download_url);
        writer.WriteStringValue("git_url", Git_url);
        writer.WriteStringValue("html_url", Html_url);
        writer.WriteStringValue("name", Name);
        writer.WriteStringValue("path", Path);
        writer.WriteStringValue("sha", Sha);
        writer.WriteIntValue("size", Size);
        writer.WriteStringValue("type", Type);
        writer.WriteStringValue("url", Url);
        writer.WriteAdditionalData(AdditionalData);
    }
}
