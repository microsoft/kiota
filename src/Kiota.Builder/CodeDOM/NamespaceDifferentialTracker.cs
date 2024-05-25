using System;
using System.Collections.Generic;

namespace Kiota.Builder.CodeDOM;

public record NamespaceDifferentialTracker
{
    public int UpwardsMovesCount
    {
        get; init;
    }
    public IEnumerable<string> DownwardsSegments { get; init; } = Array.Empty<string>();
    private bool AnyUpwardsMove => UpwardsMovesCount > 0;
    private bool AnySegment
    {
        get
        {
            if (DownwardsSegments != null)
            {
                foreach (var segment in DownwardsSegments)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public NamespaceDifferentialTrackerState State => (AnyUpwardsMove, AnySegment) switch
    {
        (true, true) => NamespaceDifferentialTrackerState.UpwardsAndThenDownwards,
        (true, false) => NamespaceDifferentialTrackerState.Upwards,
        (false, true) => NamespaceDifferentialTrackerState.Downwards,
        (false, false) => NamespaceDifferentialTrackerState.Same,
    };
}
public enum NamespaceDifferentialTrackerState
{
    Upwards,
    Downwards,
    Same,
    UpwardsAndThenDownwards,
}
