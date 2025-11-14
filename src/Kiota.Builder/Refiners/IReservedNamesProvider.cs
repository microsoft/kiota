using System.Collections.Generic;

namespace Kiota.Builder.Refiners;

public interface IReservedNamesProvider
{
    HashSet<string> ReservedNames
    {
        get;
    }
}
