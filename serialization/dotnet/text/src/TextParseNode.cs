using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Text;
/// <summary>
/// The <see cref="IParseNode"/> implementation for the json content type
/// </summary>
public class TextParseNode : IParseNode
{
    private readonly string Text;
    /// <summary>
    /// Initializes a new instance of the <see cref="TextParseNode"/> class.
    /// </summary>
    /// <param name="text">The text value.</param>
    public TextParseNode(string text)
    {
        Text = text?.Trim('"');
    }
    /// <inheritdoc />
    public Action<IParsable> OnBeforeAssignFieldValues { get; set; }
    /// <inheritdoc />
    public Action<IParsable> OnAfterAssignFieldValues { get; set; }
    /// <inheritdoc />
    public bool? GetBoolValue() => bool.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public byte[] GetByteArrayValue() => string.IsNullOrEmpty(Text) ? null : Convert.FromBase64String(Text);
    /// <inheritdoc />
    public byte? GetByteValue() => byte.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public IParseNode GetChildNode(string identifier) => throw new InvalidOperationException("text does not support structured data");
    /// <inheritdoc />
    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable => throw new InvalidOperationException("text does not support structured data");
    /// <inheritdoc />
    public IEnumerable<T> GetCollectionOfPrimitiveValues<T>() => throw new InvalidOperationException("text does not support structured data");
    /// <inheritdoc />
    public DateTimeOffset? GetDateTimeOffsetValue() => DateTimeOffset.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public Date? GetDateValue() => DateTime.TryParse(Text, out var result) ? new Date(result) : null;
    /// <inheritdoc />
    public decimal? GetDecimalValue() => decimal.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public double? GetDoubleValue() => double.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public float? GetFloatValue() => float.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public Guid? GetGuidValue() => Guid.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public int? GetIntValue() => int.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public long? GetLongValue() => long.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable => throw new InvalidOperationException("text does not support structured data");
    /// <inheritdoc />
    public sbyte? GetSbyteValue() => sbyte.TryParse(Text, out var result) ? result : null;
    /// <inheritdoc />
    public string GetStringValue() => Text;
    /// <inheritdoc />
    public TimeSpan? GetTimeSpanValue() => string.IsNullOrEmpty(Text) ? null : XmlConvert.ToTimeSpan(Text);
    /// <inheritdoc />
    public Time? GetTimeValue() => DateTime.TryParse(Text, out var result) ? new Time(result) : null;
    /// <inheritdoc />
    IEnumerable<T?> IParseNode.GetCollectionOfEnumValues<T>() => throw new InvalidOperationException("text does not support structured data");
    /// <inheritdoc />
    T? IParseNode.GetEnumValue<T>() => Enum.TryParse<T>(Text, true, out var result) ? result : null;
}
