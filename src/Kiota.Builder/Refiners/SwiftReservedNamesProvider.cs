using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners {
    public class SwiftReservedNamesProvider : IReservedNamesProvider
    {
        private readonly Lazy<HashSet<string>> _reservedNames = new(() => new HashSet<string> {
            "any"
            // TODO (Swift) add full list
        });
        public HashSet<string> ReservedNames => _reservedNames.Value;
    }
}
