using System;
using System.Collections.Generic;

namespace Kiota.Builder.SearchProviders;

#pragma warning disable CA2227
#pragma warning disable CA1002
public record SearchResult(string Title, string Description, Uri? ServiceUrl, Uri? DescriptionUrl, List<string> VersionLabels);
#pragma warning restore CA1002
#pragma warning restore CA2227
