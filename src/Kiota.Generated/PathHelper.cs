using System;
using System.IO;

internal class PathHelper
{
    internal static string Join(string first, string second)
    {
        if(string.IsNullOrEmpty(second)) return first;
        if(string.IsNullOrEmpty(first)) return second;
        return first + 
            (first.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase) || first.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase) ? 
                string.Empty :
                Path.DirectorySeparatorChar) +
            second;
    }
}
