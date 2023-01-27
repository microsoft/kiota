using System;
using System.Collections.Generic;

namespace Kiota.Builder.SearchProviders;

public record SearchResult(string Title, string Description, Uri? ServiceUrl, Uri? DescriptionUrl, List<string> VersionLabels);
