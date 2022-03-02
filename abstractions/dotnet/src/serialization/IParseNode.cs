// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Serialization
{
    /// <summary>
    /// Interface for a deserialization node in a parse tree. This interace provides an abstraction layer over serialiation formats, libararies and implementations.
    /// </summary>
    public interface IParseNode
    {
        /// <summary>
        ///  Gets the string value of the node.
        /// </summary>
        /// <returns>The string value of the node.</returns>
        string GetStringValue();
        /// <summary>
        ///  Gets a new parse node for the given identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The new parse node.</returns>
        IParseNode GetChildNode(string identifier);
        /// <summary>
        ///  Gets the boolean value of the node.
        /// </summary>
        /// <returns>The boolean value of the node.</returns>
        bool? GetBoolValue();
        /// <summary>
        ///  Gets the integer value of the node.
        /// </summary>
        /// <returns>The integer value of the node.</returns>
        int? GetIntValue();
        /// <summary>
        ///  Gets the float value of the node.
        /// </summary>
        /// <returns>The float value of the node.</returns>
        float? GetFloatValue();
        /// <summary>
        ///  Gets the long value of the node.
        /// </summary>
        /// <returns>The long value of the node.</returns>
        long? GetLongValue();
        /// <summary>
        /// Gets the double value of the node.
        /// </summary>
        /// <returns>The double value of the node.</returns>
        double? GetDoubleValue();
        /// <summary>
        /// Gets the decimal value of the node.
        /// </summary>
        /// <returns>The decimal value of the node.</returns>
        decimal? GetDecimalValue();
        /// <summary>
        /// Gets the GUID value of the node.
        /// </summary>
        /// <returns>The GUID value of the node.</returns>
        Guid? GetGuidValue();
        /// <summary>
        /// Gets the DateTimeOffset value of the node.
        /// </summary>
        /// <returns>The DateTimeOffset value of the node.</returns>
        DateTimeOffset? GetDateTimeOffsetValue();
        /// <summary>
        /// Gets the TimeSpan value of the node.
        /// </summary>
        /// <returns>The TimeSpan value of the node.</returns>
        TimeSpan? GetTimeSpanValue();
        /// <summary>
        /// Gets the Date value of the node.
        /// </summary>
        /// <returns>The Date value of the node.</returns>
        Date? GetDateValue();
        /// <summary>
        /// Gets the Time value of the node.
        /// </summary>
        /// <returns>The Time value of the node.</returns>
        Time? GetTimeValue();
        /// <summary>
        /// Gets the collection of primitive values of the node.
        /// </summary>
        /// <returns>The collection of primitive values.</returns>
        IEnumerable<T> GetCollectionOfPrimitiveValues<T>();
        /// <summary>
        /// Gets the collection of enum values of the node.
        /// </summary>
        /// <returns>The collection of enum values.</returns>
        IEnumerable<T?> GetCollectionOfEnumValues<T>() where T : struct, Enum;
        /// <summary>
        /// Gets the collection of model objects values of the node.
        /// </summary>
        /// <param name="factory">The factory to use to create the model object.</param>
        /// <returns>The collection of model objects values.</returns>
        IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable;
        /// <summary>
        /// Gets the enum value of the node.
        /// </summary>
        /// <returns>The enum value of the node.</returns>
        T? GetEnumValue<T>() where T : struct, Enum;
        /// <summary>
        /// Gets the model object value of the node.
        /// </summary>
        /// <param name="factory">The factory to use to create the model object.</param>
        /// <returns>The model object value of the node.</returns>
        T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable;
        /// <summary>
        /// Callback called before the node is deserialized.
        /// </summary>
        Action<IParsable> OnBeforeAssignFieldValues { get; set; }
        /// <summary>
        /// Callback called after the node is deserialized.
        /// </summary>
        Action<IParsable> OnAfterAssignFieldValues { get; set; }
        /// <summary>
        /// Gets the byte array value of the node.
        /// </summary>
        /// <returns>The byte array value of the node.</returns>
        byte[] GetByteArrayValue();
    }
}
