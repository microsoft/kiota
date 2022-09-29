using System;

namespace Kiota.Builder.SearchProviders;

public record SearchResult(string Title, string Description, Uri ServiceUrl, Uri DescriptionUrl);
