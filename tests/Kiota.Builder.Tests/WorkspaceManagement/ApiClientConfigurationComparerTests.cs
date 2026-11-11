using System;
using System.Collections.Generic;
using Kiota.Builder.Lock;
using Kiota.Builder.WorkspaceManagement;
using Xunit;

namespace Kiota.Builder.Tests.WorkspaceManagement;

public sealed class ApiClientConfigurationComparerTests
{
    private readonly ApiClientConfigurationComparer _comparer = new();
    [Fact]
    public void Defensive()
    {
        Assert.Equal(new HashCode().ToHashCode(), _comparer.GetHashCode(null));
        Assert.True(_comparer.Equals(null, null));
        Assert.False(_comparer.Equals(new(), null));
        Assert.False(_comparer.Equals(null, new()));
    }
    [Fact]
    public void GetsHashCode()
    {
        var iEnumComparer = new StringIEnumerableDeepComparer();
        var hash = new HashCode();
        hash.Add(new HashSet<string>(StringComparer.OrdinalIgnoreCase), iEnumComparer);
        var propertyComparer = new StringIEnumerableDeepComparer(StringComparer.Ordinal);
        hash.Add(new HashSet<string>(StringComparer.Ordinal), propertyComparer);
        hash.Add(new HashSet<string>(StringComparer.Ordinal), propertyComparer);
        var stringComparer = StringComparer.OrdinalIgnoreCase;
        hash.Add(string.Empty, stringComparer);
        hash.Add(string.Empty, stringComparer);
        hash.Add("public", stringComparer);
        hash.Add(false);
        hash.Add(true);
        hash.Add(false);
        hash.Add([], iEnumComparer);
        var hash2 = new HashCode();
        hash2.Add(string.Empty, stringComparer);
        hash2.Add(string.Empty, stringComparer);
        hash2.Add(new HashSet<string>(StringComparer.OrdinalIgnoreCase), iEnumComparer);
        hash2.Add(new HashSet<string>(StringComparer.OrdinalIgnoreCase), iEnumComparer);
        hash2.Add(new HashSet<string>(StringComparer.OrdinalIgnoreCase), iEnumComparer);
        hash.Add(hash2.ToHashCode());
        Assert.Equal(hash.ToHashCode(), _comparer.GetHashCode(new() { UsesBackingStore = true }));
    }

    [Fact]
    public void ComparesPropertySelectionsOrdinallyAndIndependentlyOfOrder()
    {
        var first = new ApiClientConfiguration();
        first.IncludeProperties.UnionWith(["User#name", "User#Name"]);
        first.ExcludeProperties.UnionWith(["User#mail", "User#id"]);
        var second = new ApiClientConfiguration();
        second.IncludeProperties.UnionWith(["User#Name", "User#name", "User#Name"]);
        second.ExcludeProperties.UnionWith(["User#id", "User#mail"]);
        Assert.True(_comparer.Equals(first, second));
        Assert.Equal(_comparer.GetHashCode(first), _comparer.GetHashCode(second));

        second.IncludeProperties.Remove("User#Name");
        second.IncludeProperties.Add("user#Name");
        Assert.False(_comparer.Equals(first, second));
        second.IncludeProperties = new(first.IncludeProperties, StringComparer.Ordinal);
        second.ExcludeProperties.Remove("User#id");
        second.ExcludeProperties.Add("User#Id");
        Assert.False(_comparer.Equals(first, second));
    }

    [Fact]
    public void ConsidersAllowedExternalOrigins()
    {
        Assert.False(_comparer.Equals(
            new()
            {
                AllowedExternalOrigins = ["https://contoso.com/*"]
            },
            new()
            {
                AllowedExternalOrigins = ["https://example.com/*"]
            }));
    }
}
