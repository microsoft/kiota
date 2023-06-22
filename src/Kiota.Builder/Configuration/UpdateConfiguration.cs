using System;

namespace Kiota.Builder.Configuration;

public class UpdateConfiguration : ICloneable
{
    public string OrgName { get; set; } = "microsoft";
    public string RepoName { get; set; } = "kiota";
    public bool Disabled
    {
        get; set;
    }
    public object Clone()
    {
        return new UpdateConfiguration
        {
            OrgName = OrgName,
            RepoName = RepoName,
            Disabled = Disabled
        };
    }
}
