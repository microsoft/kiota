using Microsoft.OpenApi.Models;

namespace kiota.Rpc
{
    internal class SecurityRequirementMapper
    {
        public static IDictionary<string, SecurityRequirement>? FromSecurityRequirementList(IList<OpenApiSecurityRequirement>? securityRequirementList)
        {
            var requirements = new Dictionary<string, SecurityRequirement>();
            if (securityRequirementList is null) return null;

            foreach (var securityRequirement in securityRequirementList)
            {
                if (securityRequirement is null) continue;

                foreach (var securityRequirementItem in securityRequirement)
                {
                    if (securityRequirementItem.Key.Reference is null) continue;
                    if (securityRequirementItem.Value is null) continue;
                    if (securityRequirementItem.Key.Reference.Id is null) continue;

                    string name = securityRequirementItem.Key.Reference.Id;
                    var scopes = new List<string>();
                    foreach (var scope in securityRequirementItem.Value)
                    {
                        scopes.Add(scope);
                    }
                    var requirementItem = new SecurityRequirement(scopes);

                    requirements.Add(name, requirementItem);
                }
            }

            return requirements;
        }
    }
}
