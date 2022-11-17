using System.Collections.Generic;

using Kiota.Builder.OpenApiExtensions;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Writers;

using Moq;

using Xunit;

namespace Kiota.Builder.Tests.OpenApiExtensions;

public class OpenApiEnumValuesDescriptionExtensionTests {
    [Fact]
    public void NOOPTestForCoverage() {
        // This class is already covered by the convertion library tests
        var value = new OpenApiEnumValuesDescriptionExtension
        {
            EnumName = "some enum",
            ValuesDescriptions = new List<EnumDescription>
            {
                new EnumDescription
                {
                    Value = "some value",
                },
            },
        };
        var writer = new Mock<IOpenApiWriter>();
        value.Write(writer.Object, OpenApiSpecVersion.OpenApi3_0);
        writer.Verify(static x => x.WriteStartObject(), Times.AtLeastOnce());
    }
}

