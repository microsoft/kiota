using System;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

using Moq;

using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Go)]
    public async Task CreatesBackwardCompatibleFactoryWhileSharedModelPropertiesAreBuildingAsync(GenerationLanguage language)
    {
        await using var stream = await GetDocumentStreamAsync("""
openapi: 3.0.4
info:
  title: Shared response
  version: 1.0.0
servers:
  - url: https://example.com
paths:
  /first:
    get:
      responses:
        '200':
          description: Shared response
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SharedGetResponse'
  /second:
    get:
      responses:
        '200':
          description: Shared response
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SharedGetResponse'
components:
  schemas:
    SharedGetResponse:
      type: object
      properties:
        value:
          type: string
""");
        var executorCreated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseProperty = new ManualResetEventSlim();
        var propertyBuilding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedBuilders = 0;
        var logger = new Mock<ILogger<KiotaBuilder>>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                var message = invocation.Arguments[2].ToString()!;
                if (message.StartsWith("Creating class ", StringComparison.Ordinal) &&
                    message.EndsWith("RequestBuilder", StringComparison.Ordinal) &&
                    Interlocked.Increment(ref startedBuilders) > 1 &&
                    !propertyBuilding.Task.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken))
                    throw new TimeoutException("The shared model property did not start building.");
                if (propertyBuilding.Task.IsCompleted && invocation.Arguments[2].ToString()!.StartsWith("Creating method ", StringComparison.Ordinal))
                    executorCreated.TrySetResult();
            }));
        var builder = new KiotaBuilder(logger.Object, new GenerationConfiguration
        {
            Language = language,
            ExcludeBackwardCompatible = false,
            MaxDegreeOfParallelism = 2,
        }, _httpClient);
        var document = await builder.CreateOpenApiDocumentAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
        for (var index = 0; index < 6; index++)
            document.Paths!.Add($"/other{index}", document.Paths["/second"]);
        var property = new Mock<IOpenApiSchema>();
        property.SetupGet(x => x.Type).Returns(JsonSchemaType.String);
        property.SetupGet(x => x.Description).Returns(() =>
        {
            // Pause the first endpoint after its model is published, before its properties finish.
            propertyBuilding.TrySetResult();
            if (!releaseProperty.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken))
                throw new TimeoutException("The shared model property was not released.");
            return "Shared value";
        });
        document.Components!.Schemas!["SharedGetResponse"].Properties!["value"] = property.Object;
        var node = builder.CreateUriSpace(document);
        builder.SetApiRootUrl();
        var generation = Task.Factory.StartNew(() => builder.CreateSourceModel(node), TestContext.Current.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        CodeNamespace result;
        try
        {
            await propertyBuilding.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            // The other endpoint must clone the factory while property creation remains paused.
            await executorCreated.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
        finally
        {
            releaseProperty.Set();
            result = await generation;
        }
        var model = result.FindChildByName<CodeClass>("SharedGetResponse", true);
        var obsolete = result.FindChildByName<CodeClass>("SharedResponse", true);
        Assert.NotNull(model);
        Assert.NotNull(obsolete);
        var factory = Assert.Single(model.Methods, x => x.Kind is CodeMethodKind.Factory);
        var obsoleteFactory = Assert.Single(obsolete.Methods, x => x.Kind is CodeMethodKind.Factory);
        Assert.Same(model, ((CodeType)factory.ReturnType).TypeDefinition);
        Assert.Same(obsolete, ((CodeType)obsoleteFactory.ReturnType).TypeDefinition);
        Assert.Equal("IParseNode", Assert.Single(obsoleteFactory.Parameters).Type.Name);
        Assert.Contains(model.Properties, x => x.Name.Equals("value", StringComparison.OrdinalIgnoreCase));
        foreach (var name in new[] { "first", "second", "other0", "other1", "other2", "other3", "other4", "other5" })
        {
            var requestBuilder = result.FindChildByName<CodeClass>($"{name}RequestBuilder", true);
            Assert.NotNull(requestBuilder);
            Assert.Single(requestBuilder.Methods, x => x.Kind is CodeMethodKind.RequestExecutor && x.Deprecation?.IsDeprecated == true);
        }
    }
}
