using System;
using System.Collections.Generic;

namespace Kiota.Builder.WorkspaceManagement;

#pragma warning disable CA2227 // Collection properties should be read only
public class ApiPluginConfiguration : BaseApiConsumerConfiguration, ICloneable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiPluginConfiguration"/> class.
    /// </summary>
    public ApiPluginConfiguration() : base()
    {

    }
    public HashSet<string> Types { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public object Clone()
    {
        var result = new ApiPluginConfiguration()
        {
            Types = new HashSet<string>(Types, StringComparer.OrdinalIgnoreCase)
        };
        CloneBase(result);
        return result;
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
