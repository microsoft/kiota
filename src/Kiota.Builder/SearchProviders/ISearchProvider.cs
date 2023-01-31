﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.SearchProviders;

public interface ISearchProvider
{
    Task<IDictionary<string, SearchResult>> SearchAsync(string term, string? version, CancellationToken cancellationToken);
    string ProviderKey
    {
        get;
    }
    HashSet<string> KeysToExclude
    {
        get; set;
    }
}
