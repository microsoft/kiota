using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;

namespace Kiota.Builder.WorkspaceManagement;
/// <summary>
/// Compares two <see cref="ApiClientConfiguration"/> instances.
/// </summary>
public class ApiClientConfigurationComparer : BaseApiConsumerConfigurationComparer<ApiClientConfiguration>
{
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();
    /// <inheritdoc/>
    public override int GetHashCode([DisallowNull] ApiClientConfiguration obj)
    {
        if (obj == null) return 0;
        return
            _stringIEnumerableDeepComparer.GetHashCode(obj.DisabledValidationRules?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 37 +
            (string.IsNullOrEmpty(obj.ClientNamespaceName) ? 0 : obj.ClientNamespaceName.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 31 +
            (string.IsNullOrEmpty(obj.Language) ? 0 : obj.Language.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 23 +
            obj.ExcludeBackwardCompatible.GetHashCode() * 19 +
            obj.UsesBackingStore.GetHashCode() * 17 +
            obj.IncludeAdditionalData.GetHashCode() * 13 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.StructuredMimeTypes?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 11 +
            base.GetHashCode(obj);
    }
}
