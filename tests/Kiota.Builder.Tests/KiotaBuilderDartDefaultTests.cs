using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kiota.Builder.Tests;

public sealed partial class KiotaBuilderTests
{
    [Theory]
    [InlineData(GenerationLanguage.Dart, 1)]
    [InlineData(GenerationLanguage.CSharp, 0)]
    public async Task WarnsOnUnsupportedDartDefaultWithoutLoggingItsPayload(GenerationLanguage language, int warningCount)
    {
        var root = CodeNamespace.InitRootNamespace();
        var models = root.AddNamespace("client.models");
        var model = models.AddClass(new CodeClass { Name = "Settings", Kind = CodeClassKind.Model }).First();
        model.AddProperty(
            new CodeProperty { Name = "unsupported", Kind = CodePropertyKind.Custom, Type = new CodeType { Name = "Object", IsExternal = true }, DefaultValue = "\"injectedCall()\"" },
            new CodeProperty { Name = "supported", Kind = CodePropertyKind.Custom, Type = new CodeType { Name = "string", IsExternal = true }, DefaultValue = "\"value\"" });
        var logger = new kiota.Rpc.FakeLogger<KiotaBuilder>();
        var configuration = new GenerationConfiguration { Language = language };
        var builder = new KiotaBuilder(logger, configuration, _httpClient);

        await builder.ApplyLanguageRefinementAsync(configuration, root, TestContext.Current.CancellationToken);

        var warnings = logger.LogEntries.Where(static x => x.level == LogLevel.Warning).ToArray();
        Assert.Equal(warningCount, warnings.Length);
        Assert.DoesNotContain(logger.LogEntries, static x => x.message.Contains("injectedCall", System.StringComparison.Ordinal));
        if (warningCount > 0)
            Assert.Contains("Ignoring the default value for property unsupported", warnings[0].message);
    }

    [Fact]
    public async Task WarnsOnInvalidEnumAndRuntimeParameterDartDefaults()
    {
        var root = CodeNamespace.InitRootNamespace();
        var models = root.AddNamespace("client.models");
        var model = models.AddClass(new CodeClass { Name = "Settings", Kind = CodeClassKind.Model }).First();
        var codeEnum = models.AddEnum(new CodeEnum { Name = "Status" }).First();
        codeEnum.AddOption(new CodeEnumOption { Name = "value", SerializationName = "value" });
        model.AddProperty(
            new CodeProperty { Name = "invalid", Kind = CodePropertyKind.Custom, Type = new CodeType { TypeDefinition = codeEnum }, DefaultValue = "\"valueEscaped\"" },
            new CodeProperty { Name = "valid", Kind = CodePropertyKind.Custom, Type = new CodeType { TypeDefinition = codeEnum }, DefaultValue = "\"value\"" });
        var constructor = model.AddMethod(new CodeMethod { Name = "constructor", Kind = CodeMethodKind.Constructor, ReturnType = new CodeType { Name = "void" }, IsAsync = false }).First();
        constructor.AddParameter(new CodeParameter { Name = "date", Type = new CodeType { Name = "DateTime", IsExternal = true }, DefaultValue = "\"2026-01-01\"" });
        var logger = new kiota.Rpc.FakeLogger<KiotaBuilder>();
        var configuration = new GenerationConfiguration { Language = GenerationLanguage.Dart };
        var builder = new KiotaBuilder(logger, configuration, _httpClient);

        await builder.ApplyLanguageRefinementAsync(configuration, root, TestContext.Current.CancellationToken);

        var warnings = logger.LogEntries.Where(static x => x.level == LogLevel.Warning).ToArray();
        Assert.Equal(2, warnings.Length);
        Assert.Contains(warnings, static x => x.message.Contains("property invalid", System.StringComparison.Ordinal));
        Assert.Contains(warnings, static x => x.message.Contains(".date", System.StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, static x => x.message.Contains("2026-01-01", System.StringComparison.Ordinal));
    }
}
