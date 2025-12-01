using System;
using System.Collections.Generic;
using Kiota.Builder.Refiners;

namespace Kiota.Builder;

public class StructuralPropertiesReservedNameProvider : IReservedNamesProvider
{
    private readonly Lazy<HashSet<string>> _reservedNames = new(static () => new(StringComparer.OrdinalIgnoreCase)
    {
        "GetFieldDeserializers",
        "get_field_deserializers",
        "Serialize",
        "AdditionalData",
        "additional_data",
        "BackingStore",
        "backing_store",
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
