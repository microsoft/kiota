using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Refiners;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.Dart;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Dart;

public class DartConventionServiceTests
{
    private readonly DartConventionService conventions = new();
    private readonly CodeClass model = new() { Name = "Settings", Kind = CodeClassKind.Model };

    [Theory]
    [InlineData("String", "\"\"", "''")]
    [InlineData("String", "\"\"quoted\"\"", "'\"quoted\"'")]
    [InlineData("String", "\"'quoted'\"", "'\\'quoted\\''")]
    [InlineData("String", "\"null\"", "'null'")]
    [InlineData("boolean", "\"TRUE\"", "true")]
    [InlineData("bool", "false", "false")]
    [InlineData("int", "-123", "-123")]
    [InlineData("int64", "9223372036854775807", "9223372036854775807")]
    [InlineData("long", "9223372036854775807", "9223372036854775807")]
    [InlineData("number", "1.25e2", "1.25e2")]
    [InlineData("double", "1.25e2", "1.25e2")]
    [InlineData("DateTime", "\"2026-01-01T00:00:00Z\"", "DateTime.parse('2026-01-01T00:00:00Z')")]
    [InlineData("DateTime", "\"20120227T132700\"", "DateTime.parse('20120227T132700')")]
    [InlineData("DateTime", "\"2012-02-27 13:27:00,123456789z\"", "DateTime.parse('2012-02-27 13:27:00,123456789z')")]
    [InlineData("DateTime", "\"2002-02-27T14:00:00-0500\"", "DateTime.parse('2002-02-27T14:00:00-0500')")]
    [InlineData("DateTime", "\"2020-01-42\"", "DateTime.parse('2020-01-42')")]
    [InlineData("DateTime", "\"-123450101 00:00:00 Z\"", "DateTime.parse('-123450101 00:00:00 Z')")]
    [InlineData("DateTime", "\"+275760-09-13T00:00:00Z\"", "DateTime.parse('+275760-09-13T00:00:00Z')")]
    [InlineData("DateTime", "\"-271821-04-20T00:00:00Z\"", "DateTime.parse('-271821-04-20T00:00:00Z')")]
    [InlineData("DateTime", "\"0000-01-01T00:00:00Z\"", "DateTime.parse('0000-01-01T00:00:00Z')")]
    [InlineData("DateTime", "\"2026-00-00T99:99:99Z\"", "DateTime.parse('2026-00-00T99:99:99Z')")]
    [InlineData("DateTime", "\"+275760-09-13T01:00:00+01:00\"", "DateTime.parse('+275760-09-13T01:00:00+01:00')")]
    [InlineData("DateOnly", "\"2026-01-01\"", "DateOnly.fromDateTimeString('2026-01-01')")]
    [InlineData("DateOnly", "\"20240229\"", "DateOnly.fromDateTimeString('20240229')")]
    [InlineData("DateOnly", "\"2026-01-01T12:30:00Z\"", "DateOnly.fromDateTimeString('2026-01-01T12:30:00Z')")]
    [InlineData("TimeOnly", "\"12:30:00\"", "TimeOnly.fromDateTimeString('12:30:00')")]
    [InlineData("TimeOnly", "\"12:30:00.123456789+01:30\"", "TimeOnly.fromDateTimeString('12:30:00.123456789+01:30')")]
    [InlineData("TimeOnly", "\"1230\"", "TimeOnly.fromDateTimeString('1230')")]
    [InlineData("TimeOnly", "\"12\"", "TimeOnly.fromDateTimeString('12')")]
    [InlineData("UuidValue", "\"d4f987cf-b557-433c-9ccb-306d85f24d76\"", "UuidValue.fromString('d4f987cf-b557-433c-9ccb-306d85f24d76')")]
    [InlineData("UuidValue", "\"D4F987CF-B557-433C-9CCB-306D85F24D76\"", "UuidValue.fromString('D4F987CF-B557-433C-9CCB-306D85F24D76')")]
    [InlineData("UuidValue", "\"00000000-0000-0000-0000-000000000000\"", "UuidValue.fromString('00000000-0000-0000-0000-000000000000')")]
    public void FormatsSupportedDefaults(string typeName, string input, string expected)
    {
        Assert.True(conventions.TryGetDefaultValue(new CodeType { Name = typeName }, input, model, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("bool", "false; injectedCall()")]
    [InlineData("bool", "''true''")]
    [InlineData("int", "1.5")]
    [InlineData("byte", "256")]
    [InlineData("int64", "9223372036854775808")]
    [InlineData("long", "9223372036854775808")]
    [InlineData("number", "1e400")]
    [InlineData("float", "1e100")]
    [InlineData("double", "NaN")]
    [InlineData("double", "Infinity")]
    [InlineData("double", "1e400")]
    [InlineData("double", "1; injectedCall()")]
    [InlineData("Object", "\"injectedCall()\"")]
    [InlineData("UnknownModel", "\"injectedCall()\"")]
    [InlineData("DateTime", "\"not-a-date\"")]
    [InlineData("DateTime", "\"2026-01-01garbage\"")]
    [InlineData("DateTime", "\"2026-01-01t12:30:00Z\"")]
    [InlineData("DateTime", "\"2026-01-01\n\"")]
    [InlineData("DateTime", "\"2026-01-01T00:00:00+01:30garbage\"")]
    [InlineData("DateTime", "\"+275760-09-13T00:00:00.000001Z\"")]
    [InlineData("DateTime", "\"-271821-04-19T23:59:59.999999Z\"")]
    [InlineData("DateTime", "\"999999-01-01\"")]
    [InlineData("DateTime", "\"+275761-01-01T00:00:00Z\"")]
    [InlineData("DateTime", "\"+275760-09-13T00:00:00\"")]
    [InlineData("DateTime", "\"-271821-04-20T00:00:00+00:01\"")]
    [InlineData("DateOnly", "\"not-a-date\"")]
    [InlineData("DateOnly", "\"12:30:00\"")]
    [InlineData("TimeOnly", "\"not-a-time\"")]
    [InlineData("TimeOnly", "\"2026-01-01T12:30:00\"")]
    [InlineData("TimeOnly", "\"12:30:00\n\"")]
    [InlineData("UuidValue", "\"not-a-uuid\"")]
    [InlineData("UuidValue", "\"{d4f987cf-b557-433c-9ccb-306d85f24d76}\"")]
    [InlineData("UuidValue", "\"d4f987cfb557433c9ccb306d85f24d76\"")]
    [InlineData("UuidValue", "\"d4f987cf-b557-433c-9ccb-306d85f24d76\n\"")]
    [InlineData("DateTime", "\"quote'\"line\n\r\t\\$value\"")]
    [InlineData("DateOnly", "\"quote'\"line\n\r\t\\$value\"")]
    [InlineData("TimeOnly", "\"quote'\"line\n\r\t\\$value\"")]
    [InlineData("UuidValue", "\"quote'\"line\n\r\t\\$value\"")]
    public void RejectsUnsupportedOrMalformedDefaults(string typeName, string input)
    {
        Assert.False(conventions.TryGetDefaultValue(new CodeType { Name = typeName }, input, model, out var value));
        Assert.Empty(value);
    }

    [Theory]
    [InlineData("String")]
    [InlineData("DateTime")]
    [InlineData("int")]
    public void RejectsResolvedModelsNamedLikePrimitives(string name)
    {
        var type = new CodeType { TypeDefinition = new CodeClass { Name = name, Kind = CodeClassKind.Model } };
        Assert.False(conventions.TryGetDefaultValue(type, "\"1\"", model, out _));
    }

    [Fact]
    public void RejectsComposedCollectionsAndActions()
    {
        Assert.False(conventions.TryGetDefaultValue(new CodeUnionType { Name = "Union" }, "injectedCall()", model, out _));
        foreach (var kind in new[] { CodeTypeBase.CodeTypeCollectionKind.Array, CodeTypeBase.CodeTypeCollectionKind.Complex })
            Assert.False(conventions.TryGetDefaultValue(new CodeType { Name = "String", CollectionKind = kind }, "\"value\"", model, out _));
        Assert.False(conventions.TryGetDefaultValue(new CodeType { Name = "String", ActionOf = true }, "\"value\"", model, out _));
    }

    [Theory]
    [InlineData("String")]
    [InlineData("Object")]
    [InlineData("bool")]
    public void AcceptsOnlyNullableBareNull(string name)
    {
        var type = new CodeType { Name = name, IsNullable = false };
        Assert.False(conventions.TryGetDefaultValue(type, "null", model, out _));
        type.IsNullable = true;
        Assert.True(conventions.TryGetDefaultValue(type, "null", model, out var value));
        Assert.Equal("null", value);
        type.CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array;
        Assert.True(conventions.TryGetDefaultValue(type, "null", model, out value));
        Assert.Equal("null", value);
    }

    [Fact]
    public void EscapesStringContentsExactlyOnceForEachQuoteStyle()
    {
        const string input = "\"a'\"b\n\r\t\\$value\"";
        var type = new CodeType { Name = "String" };
        Assert.True(conventions.TryGetDefaultValue(type, input, model, out var single));
        Assert.Equal("'a\\'\"b\\n\\r\\t\\\\\\$value'", single);
        Assert.True(conventions.TryGetDefaultValue(type, input, model, out var dual, doubleQuoted: true));
        Assert.Equal("\"a'\\\"b\\n\\r\\t\\\\\\$value\"", dual);
    }

    [Theory]
    [InlineData("DateTime")]
    [InlineData("DateOnly")]
    [InlineData("TimeOnly")]
    [InlineData("UuidValue")]
    public void RejectsRuntimeParsingInConstantDefaults(string name)
    {
        Assert.False(conventions.TryGetDefaultValue(new CodeType { Name = name }, "\"value\"", model, out _, constantOnly: true));
    }

    [Theory]
    [InlineData("DateTime", "\"2026-01-01T00:00:00Z\"")]
    [InlineData("DateOnly", "\"2026-01-01\"")]
    [InlineData("TimeOnly", "\"12:30:00\"")]
    [InlineData("UuidValue", "\"d4f987cf-b557-433c-9ccb-306d85f24d76\"")]
    public void RejectsValidParsedDefaultsInConstantParameterSignatures(string name, string input)
    {
        Assert.False(conventions.TryGetDefaultValue(new CodeType { Name = name }, input, model, out var value, constantOnly: true));
        Assert.Empty(value);
    }

    [Fact]
    public void ResolvesOnlyCanonicalEnumMembersIncludingAliases()
    {
        var root = CodeNamespace.InitRootNamespace();
        root.AddClass(model);
        var codeEnum = root.AddNamespace("other").AddEnum(new CodeEnum { Name = "Status" }).First();
        codeEnum.AddOption(new CodeEnumOption { Name = "valid", SerializationName = "VALID" });
        var type = new CodeType { TypeDefinition = codeEnum };
        var property = model.AddProperty(new CodeProperty { Name = "status", Type = type }).First();
        model.AddUsing(new CodeUsing { Name = "Status", Alias = "other", Declaration = new CodeType { TypeDefinition = codeEnum } });
        Assert.True(conventions.TryGetDefaultValue(type, "valid", property, out var value));
        Assert.Equal("other.Status.valid", value);
        Assert.False(conventions.TryGetDefaultValue(type, "\"valid\"", property, out _));
        Assert.False(conventions.TryGetDefaultValue(type, "valid; injectedCall()", property, out _));
    }

    [Theory]
    [InlineData("VALID", "\"VALID\"", true)]
    [InlineData("VALID", "'VALID'", true)]
    [InlineData("VALID", "VALID", true)]
    [InlineData("VALID", "\"valid\"", false)]
    [InlineData("VALID", "\"VALID; injectedCall()\"", false)]
    [InlineData("null", "\"null\"", true)]
    [InlineData("quote'\"line\n\r\t\\$value", "\"quote'\"line\n\r\t\\$value\"", true)]
    public async Task RefinesEnumParameterWireDefaultsBeforeWritingCanonicalMembers(string wireName, string input, bool accepted)
    {
        var root = CodeNamespace.InitRootNamespace();
        var models = root.AddNamespace("models");
        models.AddClass(model);
        var codeEnum = models.AddEnum(new CodeEnum { Name = "Status" }).First();
        var option = new CodeEnumOption { Name = "valid", SerializationName = wireName };
        codeEnum.AddOption(option);
        var method = model.AddMethod(new CodeMethod { Name = "constructor", Kind = CodeMethodKind.Constructor, ReturnType = new CodeType { Name = "void" }, IsAsync = false }).First();
        var parameter = new CodeParameter { Name = "status", Type = new CodeType { TypeDefinition = codeEnum, IsNullable = false }, DefaultValue = input };
        method.AddParameter(parameter);
        var logger = new kiota.Rpc.FakeLogger<KiotaBuilder>();

        await new DartRefiner(new GenerationConfiguration { Language = GenerationLanguage.Dart }, logger).RefineAsync(root, TestContext.Current.CancellationToken);

        Assert.Equal(accepted ? option.Name : input, parameter.DefaultValue);
        Assert.Equal(accepted ? $"{{Status status = Status.{option.Name}}}" : "{Status? status}", conventions.GetParameterSignature(parameter, method));
        Assert.Equal(accepted ? 0 : 1, logger.LogEntries.Count(static x => x.level == Microsoft.Extensions.Logging.LogLevel.Warning));
        Assert.DoesNotContain(logger.LogEntries, static x => x.message.Contains("injectedCall", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResolvesCollidingEnumParameterWireNamesWithoutRemappingPropertyDefaults()
    {
        var root = CodeNamespace.InitRootNamespace();
        var models = root.AddNamespace("models");
        models.AddClass(model);
        var codeEnum = models.AddEnum(new CodeEnum { Name = "Status" }).First();
        var first = new CodeEnumOption { Name = "value", SerializationName = "value" };
        var second = new CodeEnumOption { Name = "valueEscaped", SerializationName = "valueEscaped" };
        codeEnum.AddOption(first, second);
        var property = model.AddProperty(new CodeProperty { Name = "status", Kind = CodePropertyKind.Custom, Type = new CodeType { TypeDefinition = codeEnum }, DefaultValue = "\"value\"" }).First();
        var method = model.AddMethod(new CodeMethod { Name = "constructor", Kind = CodeMethodKind.Constructor, ReturnType = new CodeType { Name = "void" }, IsAsync = false }).First();
        var parameter = new CodeParameter { Name = "status", Type = new CodeType { TypeDefinition = codeEnum, IsNullable = false }, DefaultValue = "\"valueEscaped\"" };
        method.AddParameter(parameter);

        await new DartRefiner(new GenerationConfiguration { Language = GenerationLanguage.Dart }).RefineAsync(root, TestContext.Current.CancellationToken);

        Assert.NotEqual(first.Name, second.Name);
        Assert.True(conventions.TryGetPropertyDefaultValue(property, method, out var propertyDefault));
        Assert.Equal($"Status.{first.Name}", propertyDefault);
        Assert.Equal($"{{Status status = Status.{second.Name}}}", conventions.GetParameterSignature(parameter, method));
    }

    [Fact]
    public async Task PreservesNullableEnumParameterNullInsteadOfMatchingItsWireName()
    {
        var root = CodeNamespace.InitRootNamespace();
        var models = root.AddNamespace("models");
        models.AddClass(model);
        var codeEnum = models.AddEnum(new CodeEnum { Name = "Status" }).First();
        codeEnum.AddOption(new CodeEnumOption { Name = "none", SerializationName = "null" });
        var method = model.AddMethod(new CodeMethod { Name = "constructor", Kind = CodeMethodKind.Constructor, ReturnType = new CodeType { Name = "void" }, IsAsync = false }).First();
        var parameter = new CodeParameter { Name = "status", Type = new CodeType { TypeDefinition = codeEnum, IsNullable = true }, DefaultValue = "null" };
        method.AddParameter(parameter);

        await new DartRefiner(new GenerationConfiguration { Language = GenerationLanguage.Dart }).RefineAsync(root, TestContext.Current.CancellationToken);

        Assert.Equal("null", parameter.DefaultValue);
        Assert.Equal("{Status? status = null}", conventions.GetParameterSignature(parameter, method));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RefinesEnumParametersWithoutDefaults(string defaultValue)
    {
        var root = CodeNamespace.InitRootNamespace();
        var models = root.AddNamespace("models");
        models.AddClass(model);
        var codeEnum = models.AddEnum(new CodeEnum { Name = "Status" }).First();
        codeEnum.AddOption(new CodeEnumOption { Name = "none", SerializationName = "null" });
        var method = model.AddMethod(new CodeMethod { Name = "constructor", Kind = CodeMethodKind.Constructor, ReturnType = new CodeType { Name = "void" }, IsAsync = false }).First();
        var parameter = new CodeParameter { Name = "status", Type = new CodeType { TypeDefinition = codeEnum, IsNullable = false }, DefaultValue = defaultValue };
        method.AddParameter(parameter);
        var logger = new kiota.Rpc.FakeLogger<KiotaBuilder>();

        await new DartRefiner(new GenerationConfiguration { Language = GenerationLanguage.Dart }, logger).RefineAsync(root, TestContext.Current.CancellationToken);

        Assert.Equal(defaultValue, parameter.DefaultValue);
        Assert.Equal("Status status", conventions.GetParameterSignature(parameter, method));
        Assert.DoesNotContain(logger.LogEntries, static x => x.level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GuardsNullablePathStringsWithConsistentSpacingAndEscapedKeys(bool nullable)
    {
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, ".", "client");
        using var output = new StringWriter();
        writer.SetTextWriter(output);
        var type = new CodeType { Name = "String", IsNullable = nullable };

        conventions.AddParametersAssignment(writer, new CodeType { Name = "Map<String, dynamic>" }, "pathParameters", "pathParameters",
            (type, "quote\"'\\\n\r\t$value", "id"));

        var expectedGuard = nullable ? "if (id != null && id.isNotEmpty) " : string.Empty;
        Assert.Equal($"{expectedGuard}pathParameters[\"quote\\\"'\\\\\\n\\r\\t\\$value\"]=id;{Environment.NewLine}", output.ToString());
    }

    [Theory]
    [InlineData("int", true)]
    [InlineData("int", false)]
    [InlineData("UuidValue", true)]
    [InlineData("UuidValue", false)]
    [InlineData("String", true)]
    [InlineData("String", false)]
    public void GuardsNullablePathCollectionsWithoutChangingKeysOrSkippingEmptyCollections(string typeName, bool nullable)
    {
        var writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Dart, ".", "client");
        using var output = new StringWriter();
        writer.SetTextWriter(output);
        var type = new CodeType { Name = typeName, IsNullable = nullable, CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array };

        conventions.AddParametersAssignment(writer, new CodeType { Name = "Map<String, dynamic>" }, "pathParameters", "pathParameters",
            (type, "quote\"'\\\n\r\t$value", "ids"));

        var expectedGuard = nullable ? "if (ids != null) " : string.Empty;
        Assert.Equal($"{expectedGuard}pathParameters[\"quote\\\"'\\\\\\n\\r\\t\\$value\"]=ids;{Environment.NewLine}", output.ToString());
        Assert.DoesNotContain("isNotEmpty", output.ToString());
    }

    [Theory]
    [InlineData(CodeClassKind.RequestBuilder, CodeMethodKind.Constructor, true, true, "UuidValue? parameter")]
    [InlineData(CodeClassKind.RequestBuilder, CodeMethodKind.ClientConstructor, true, true, "UuidValue? parameter")]
    [InlineData(CodeClassKind.RequestBuilder, CodeMethodKind.RawUrlConstructor, true, true, "UuidValue? parameter")]
    [InlineData(CodeClassKind.RequestBuilder, CodeMethodKind.Constructor, false, true, "UuidValue parameter")]
    [InlineData(CodeClassKind.RequestBuilder, CodeMethodKind.Constructor, true, false, "UuidValue parameter")]
    [InlineData(CodeClassKind.RequestBuilder, CodeMethodKind.RequestBuilderWithParameters, true, true, "UuidValue parameter")]
    [InlineData(CodeClassKind.RequestBuilder, CodeMethodKind.RequestExecutor, true, true, "UuidValue parameter")]
    [InlineData(CodeClassKind.Model, CodeMethodKind.Constructor, true, true, "UuidValue parameter")]
    public void ScopesOptionalConstructorNullabilityToRequestBuilders(CodeClassKind classKind, CodeMethodKind methodKind, bool optional, bool nullable, string expected)
    {
        var parentClass = new CodeClass { Name = "Parent", Kind = classKind };
        var method = parentClass.AddMethod(new CodeMethod { Name = "method", Kind = methodKind, ReturnType = new CodeType { Name = "void" } }).First();
        var parameter = new CodeParameter { Name = "parameter", Optional = optional, Type = new CodeType { Name = "UuidValue", IsNullable = nullable } };
        method.AddParameter(parameter);

        Assert.Equal(expected, conventions.GetParameterSignature(parameter, method));
        Assert.False(conventions.IsNamedParameter(parameter, method));
    }

    [Theory]
    [InlineData(CodePropertyKind.AdditionalData, "{}")]
    [InlineData(CodePropertyKind.Options, "<RequestOption>[]")]
    [InlineData(CodePropertyKind.Headers, "HttpHeaders()")]
    public void ReconstructsOnlyGeneratedInfrastructureDefaults(CodePropertyKind kind, string expected)
    {
        var property = new CodeProperty { Name = "generated", Kind = kind, Type = new CodeType { Name = "Map<String, Object>" }, DefaultValue = "injectedCall()" };
        Assert.True(conventions.TryGetPropertyDefaultValue(property, model, out var value));
        Assert.Equal(expected, value);
        property.Kind = CodePropertyKind.Custom;
        Assert.False(conventions.TryGetPropertyDefaultValue(property, model, out _));
    }

    [Theory]
    [InlineData("String", "\"$value\"", "{String parameter = \"\\$value\"}")]
    [InlineData("String", "'$value'", "{String parameter = '\\$value'}")]
    [InlineData("bool", "TRUE", "{bool parameter = true}")]
    [InlineData("int", "12", "{int parameter = 12}")]
    [InlineData("Object", "injectedCall()", "{Object? parameter}")]
    [InlineData("DateTime", "\"2026-01-01\"", "{DateTime? parameter}")]
    public void PreservesNamedParameterGroupingForAcceptedAndRejectedDefaults(string name, string input, string expected)
    {
        var parameter = new CodeParameter { Name = "parameter", Type = new CodeType { Name = name, IsNullable = false }, DefaultValue = input };
        Assert.Equal(expected, conventions.GetParameterSignature(parameter, model));
    }

    [Fact]
    public void KeepsNullableNullParameterTypeAndOptionalStringDefault()
    {
        var parameter = new CodeParameter { Name = "parameter", Type = new CodeType { Name = "bool", IsNullable = true }, DefaultValue = "null" };
        Assert.Equal("{bool? parameter = null}", conventions.GetParameterSignature(parameter, model));
        parameter.DefaultValue = string.Empty;
        parameter.Type = new CodeType { Name = "String", IsNullable = false };
        parameter.Optional = true;
        Assert.Equal("{String parameter = \"\"}", conventions.GetParameterSignature(parameter, model));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptsQuotedNullOnlyForNullablePrimitives(bool nullable)
    {
        var type = new CodeType { Name = "int", IsNullable = nullable };
        Assert.Equal(nullable, conventions.TryGetDefaultValue(type, "'null'", model, out var value));
        Assert.Equal(nullable ? "null" : string.Empty, value);
    }
}
