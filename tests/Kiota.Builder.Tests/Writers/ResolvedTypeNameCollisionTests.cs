using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Dart;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.Java;
using Kiota.Builder.Writers.Php;
using Kiota.Builder.Writers.Python;
using Kiota.Builder.Writers.TypeScript;
using Xunit;

namespace Kiota.Builder.Tests.Writers;

/// <summary>
/// A schema may be named binary, base64 or base64url. Such a model must keep its own name rather
/// than be rendered as the language's native binary type, which would drop its fields.
/// </summary>
public class ResolvedTypeNameCollisionTests
{
    private static CodeType ResolvedModelNamed(string name)
    {
        var root = CodeNamespace.InitRootNamespace();
        var model = root.AddClass(new CodeClass { Name = name, Kind = CodeClassKind.Model }).First();
        return new CodeType { Name = name, TypeDefinition = model };
    }

    // the builder keeps the schema's own casing, so both spellings reach the writers
    public static TheoryData<string> BinaryNames => new() { "binary", "Binary", "base64", "Base64", "base64url", "Base64url" };

    private static void AssertKeepsName(string expectedName, string translated)
    {
        Assert.Equal(expectedName, translated, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(BinaryNames))]
    public void JavaKeepsAResolvedModelName(string name) =>
        AssertKeepsName(name, new JavaConventionService().TranslateType(ResolvedModelNamed(name)));

    [Theory]
    [MemberData(nameof(BinaryNames))]
    public void PythonKeepsAResolvedModelName(string name) =>
        AssertKeepsName(name, new PythonConventionService().TranslateType(ResolvedModelNamed(name)));

    [Theory]
    [MemberData(nameof(BinaryNames))]
    public void PhpKeepsAResolvedModelName(string name) =>
        AssertKeepsName(name, new PhpConventionService().TranslateType(ResolvedModelNamed(name)));

    [Theory]
    [MemberData(nameof(BinaryNames))]
    public void DartKeepsAResolvedModelName(string name) =>
        AssertKeepsName(name, new DartConventionService().TranslateType(ResolvedModelNamed(name)));

    [Theory]
    [MemberData(nameof(BinaryNames))]
    public void TypeScriptKeepsAResolvedModelName(string name) =>
        AssertKeepsName(name, new TypeScriptConventionService().TranslateType(ResolvedModelNamed(name)));

    [Theory]
    [MemberData(nameof(BinaryNames))]
    public void GoKeepsAResolvedModelName(string name) =>
        AssertKeepsName(name, new GoConventionService().TranslateType(ResolvedModelNamed(name), false));

    // csharp already decided by the definition, so it pins the behaviour the others now match
    [Theory]
    [MemberData(nameof(BinaryNames))]
    public void CSharpKeepsAResolvedModelName(string name) =>
        Assert.Contains(name, new CSharpConventionService().TranslateType(ResolvedModelNamed(name)), StringComparison.OrdinalIgnoreCase);

    [Theory]
    [InlineData("binary", "bytes", "Iterable<int>")]
    [InlineData("base64", "bytes", "Iterable<int>")]
    [InlineData("base64url", "bytes", "Iterable<int>")]
    [InlineData("string", "str", "String")]
    [InlineData("integer", "int", "int")]
    public void AnUnresolvedCoreTypeIsStillTranslated(string name, string python, string dart)
    {
        var core = new CodeType { Name = name };
        Assert.Equal(python, new PythonConventionService().TranslateType(core));
        Assert.Equal(dart, new DartConventionService().TranslateType(core));
    }
}
