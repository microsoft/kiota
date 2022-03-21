using System;
using System.IO;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Text;

/// <summary>
/// The <see cref="IParseNodeFactory"/> implementation for text/plain content types
/// </summary>
public class TextParseNodeFactory : IParseNodeFactory
{
    /// <inheritdoc />
    public string ValidContentType => "text/plain";
    /// <inheritdoc />
    public IParseNode GetRootParseNode(string contentType, Stream content) {
        if(string.IsNullOrEmpty(contentType))
            throw new ArgumentNullException(nameof(contentType));
        else if(!ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentOutOfRangeException($"expected a {ValidContentType} content type");
        
        _ = content ?? throw new ArgumentNullException(nameof(content));
        using var reader = new StreamReader(content);
        var stringContent = reader.ReadToEnd();
        return new TextParseNode(stringContent);
    }
}
