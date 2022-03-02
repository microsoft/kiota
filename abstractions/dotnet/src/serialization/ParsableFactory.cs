// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;

namespace Microsoft.Kiota.Abstractions.Serialization;

/// <summary>
/// Defines the factory for creating parsable objects.
/// </summary>
/// <param name="node">The <see cref="IParseNode">node</see> to parse use to get the discriminator value from the payload.</param>
/// <returns>The <see cref="IParsable">parsable</see> object.</returns>
/// <typeparam name="T">The type of the parsable object.</typeparam>
public delegate T ParsableFactory<T>(IParseNode node) where T : IParsable;
