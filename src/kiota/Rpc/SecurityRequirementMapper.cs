using Microsoft.OpenApi;

namespace kiota.Rpc
{
    internal class SecurityRequirementMapper
    {
        public static IList<IDictionary<string, IList<string>?>>? FromSecurityRequirementList(IList<OpenApiSecurityRequirement>? securityRequirementList)
        {
            if (securityRequirementList is null) return null;
            var requirementsList = new List<IDictionary<string, IList<string>?>>();

            foreach (var securityRequirement in securityRequirementList)
            {
                if (securityRequirement is null) continue;
                var requirements = new Dictionary<string, IList<string>?>();

                foreach (var securityRequirementItem in securityRequirement)
                {
                    if (securityRequirementItem.Key.Reference is null) continue;
                    if (securityRequirementItem.Value is null) continue;
                    if (securityRequirementItem.Key.Reference.Id is null) continue;

                    string name = securityRequirementItem.Key.Reference.Id;
                    var scopes = securityRequirementItem.Value;
                    var requirementItem = new SecurityRequirement(scopes);

                    requirements.Add(name, scopes);
                }
                requirementsList.Add(requirements);

            }
            return requirementsList;
        }
    }
}
