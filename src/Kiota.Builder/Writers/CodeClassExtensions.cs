using System.Collections.Generic;
using System.Linq;
using System;

//methods are added here as they are only valuable for the writing process and no other steps
namespace Kiota.Builder.Writers.Extensions {
    public static class CodeClassExtensions {
        public static IEnumerable<CodeProperty> GetPropertiesOfKind(this CodeClass parentClass, params CodePropertyKind[] kinds) {
            if(parentClass == null)
                return Enumerable.Empty<CodeProperty>();
            if(kinds == null || !kinds.Any())
                throw new ArgumentOutOfRangeException(nameof(kinds));
            return parentClass.Properties
                        .Where(x => x.IsOfKind(kinds))
                        .Union(parentClass.Methods
                                .Where(x => x.IsAccessor && (x.AccessedProperty?.IsOfKind(kinds) ?? false))
                                .Select(x => x.AccessedProperty))
                        .Distinct()
                        .OrderBy(x => x.Name);
        }
        public static IEnumerable<CodeMethod> GetMethodsOffKind(this CodeClass parentClass, params CodeMethodKind[] kinds) {
            if(parentClass == null)
                return Enumerable.Empty<CodeMethod>();
            if(kinds == null || !kinds.Any())
                throw new ArgumentOutOfRangeException(nameof(kinds));
            return parentClass.Methods
                        .Where(x => x.IsOfKind(kinds))
                        .Distinct()
                        .OrderBy(x => x.Name);
        }
        public static CodeProperty GetBackingStoreProperty(this CodeClass parentClass) {
            if(parentClass == null) return null;
            return (parentClass.GetGreatestGrandparent(parentClass) ?? parentClass) // the backing store is always on the uppermost class
                                    .Properties
                                    .FirstOrDefault(x => x.IsOfKind(CodePropertyKind.BackingStore));
        }
    }
}
