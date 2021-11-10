using System;

namespace Microsoft.Kiota.Serialization.Json.Tests.Mocks
{
    [Flags]
    public enum TestEnum
    {
        One = 0x00000001,
        Two = 0x00000002,
        Four = 0x00000004,
        Eight = 0x00000008,
        Sixteen = 0x00000010
    }
}
