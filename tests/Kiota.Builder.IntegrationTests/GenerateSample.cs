using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Kiota.Builder.IntegrationTests;

public sealed class GenerateSample : IDisposable
{
    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
    private readonly HttpClient _httpClient = new();
    [InlineData(GenerationLanguage.CSharp, false)]
    [InlineData(GenerationLanguage.Java, false)]
    [InlineData(GenerationLanguage.TypeScript, false)]
    [InlineData(GenerationLanguage.Go, false)]
    [InlineData(GenerationLanguage.Dart, false)]
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [InlineData(GenerationLanguage.Dart, true)]
    [Theory]
    public async Task GeneratesTodoAsync(GenerationLanguage language, bool backingStore)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var backingStoreSuffix = backingStore ? string.Empty : "BackingStore";
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("ToDoApi.yaml"),
            OutputPath = $".\\Generated\\Todo\\{language}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp, false)]
    [InlineData(GenerationLanguage.Java, false)]
    [InlineData(GenerationLanguage.TypeScript, false)]
    [InlineData(GenerationLanguage.Go, false)]
    [InlineData(GenerationLanguage.Dart, false)]
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [InlineData(GenerationLanguage.Dart, true)]
    [Theory]
    public async Task GeneratesModelWithDictionaryAsync(GenerationLanguage language, bool backingStore)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var backingStoreSuffix = backingStore ? "BackingStore" : string.Empty;
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("ModelWithDictionary.yaml"),
            OutputPath = $".\\Generated\\ModelWithDictionary\\{language}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp, false)]
    [InlineData(GenerationLanguage.Java, false)]
    [InlineData(GenerationLanguage.TypeScript, false)]
    [InlineData(GenerationLanguage.Go, false)]
    [InlineData(GenerationLanguage.Dart, false)]
    [InlineData(GenerationLanguage.Ruby, false)]
    [InlineData(GenerationLanguage.CSharp, true)]
    [InlineData(GenerationLanguage.Java, true)]
    [InlineData(GenerationLanguage.PHP, false)]
    [InlineData(GenerationLanguage.TypeScript, true)]
    [InlineData(GenerationLanguage.Dart, true)]
    [Theory]
    public async Task GeneratesResponseWithMultipleReturnFormatsAsync(GenerationLanguage language, bool backingStore)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var backingStoreSuffix = backingStore ? "BackingStore" : string.Empty;
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("ResponseWithMultipleReturnFormats.yaml"),
            OutputPath = $".\\Generated\\ResponseWithMultipleReturnFormats\\{language}{backingStoreSuffix}",
            UsesBackingStore = backingStore,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Dart)]
    [InlineData(GenerationLanguage.Ruby)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.PHP)]
    [Theory]
    public async Task GeneratesErrorsInliningParentsAsync(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("InheritingErrors.yaml"),
            OutputPath = $".\\Generated\\ErrorInlineParents\\{language}",
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Dart)]
    [InlineData(GenerationLanguage.Ruby)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.TypeScript)]
    [InlineData(GenerationLanguage.PHP)]
    [Theory]
    public async Task GeneratesCorrectEnumsAsync(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("EnumHandling.yaml"),
            OutputPath = $".\\Generated\\EnumHandling\\{language}",
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());
    }
    [InlineData(GenerationLanguage.Java)]
    [Theory]
    public async Task GeneratesIdiomaticChildrenNamesAsync(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var OutputPath = $".\\Generated\\NoUnderscoresInObjectNames\\{language}";
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("NoUnderscoresInModel.yaml"),
            OutputPath = OutputPath,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());

        var fullText = "";
        foreach (var file in Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories))
        {
            fullText += await File.ReadAllTextAsync(file, cancellationToken: TestContext.Current.CancellationToken);
        }

        Assert.Empty(Directory.GetFiles(OutputPath, "*_*", SearchOption.AllDirectories));
        Assert.DoesNotContain("_", fullText);
    }
    [InlineData(GenerationLanguage.CSharp)]
    [InlineData(GenerationLanguage.Go)]
    [InlineData(GenerationLanguage.Dart)]
    [InlineData(GenerationLanguage.Java)]
    [InlineData(GenerationLanguage.PHP)]
    [InlineData(GenerationLanguage.Python)]
    [InlineData(GenerationLanguage.Ruby)]
    // [InlineData(GenerationLanguage.TypeScript)] // TODO: the "getQueryParameter" is added to the interface V1RequestBuilderGetQueryParameters but is not getting written because removed by ReplaceRequestConfigurationsQueryParamsWithInterfaces in the refiner
    [Theory]
    public async Task GeneratesUritemplateHintsAsync(GenerationLanguage language)
    {
        var logger = LoggerFactory.Create(builder =>
        {
        }).CreateLogger<KiotaBuilder>();

        var OutputPath = $".\\Generated\\GeneratesUritemplateHints\\{language}";
        var configuration = new GenerationConfiguration
        {
            Language = language,
            OpenAPIFilePath = GetAbsolutePath("GeneratesUritemplateHints.yaml"),
            OutputPath = OutputPath,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(new());

        var fullText = "";
        foreach (var file in Directory.GetFiles(OutputPath, "*.*", SearchOption.AllDirectories))
        {
            fullText += File.ReadAllText(file);
        }

        switch (language)
        {
            case GenerationLanguage.CSharp:
                Assert.Contains("[QueryParameter(\"startDateTime\")]", fullText);
                break;
            case GenerationLanguage.Dart:
                Assert.Contains("'EndDateTime' : endDateTime", fullText);
                break;
            case GenerationLanguage.Go:
                Assert.Contains("uriparametername:\\\"startDateTime\\\"", fullText);
                break;
            case GenerationLanguage.Java:
                Assert.Contains("allQueryParams.put(\"EndDateTime\", endDateTime)", fullText);
                break;
            case GenerationLanguage.PHP:
                Assert.Contains("@QueryParameter(\"EndDateTime\")", fullText);
                break;
            case GenerationLanguage.Python:
                Assert.Contains("get_query_parameter", fullText);
                Assert.Contains("if original_name == \"end_date_time\":", fullText);
                break;
            case GenerationLanguage.Ruby:
                Assert.Contains("get_query_parameter", fullText);
                Assert.Contains("when \"start_date_time\"", fullText);
                break;
            default:
                throw new Exception($"Please implement a test-case for {language}");

        }
    }
    [InlineData("ToDoApi.yaml")]
    [InlineData("ModelWithDictionary.yaml")]
    [InlineData("ModelWithDerivedTypes.yaml")]
    [InlineData("ResponseWithMultipleReturnFormats.yaml")]
    [InlineData("InheritingErrors.yaml")]
    [InlineData("EnumHandling.yaml")]
    [InlineData("FlagsEnumHandling.yaml")]
    [InlineData("GeneratesUritemplateHints.yaml")]
    [InlineData("SwaggerPetStore.json")]
    [Theory]
    public async Task GeneratedGoCodeIsFormattedAsync(string descriptionFile)
    {
        var gofmt = GetGoFmtPath();
        Assert.SkipWhen(string.IsNullOrEmpty(gofmt), "gofmt (the Go toolchain) is not available on this machine.");

        var logger = LoggerFactory.Create(static builder => { }).CreateLogger<KiotaBuilder>();

        var descriptionName = Path.GetFileNameWithoutExtension(descriptionFile);
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Generated", "GoFormatting", descriptionName);
        var configuration = new GenerationConfiguration
        {
            Language = GenerationLanguage.Go,
            OpenAPIFilePath = GetAbsolutePath(descriptionFile),
            OutputPath = outputPath,
            CleanOutput = true,
        };
        await new KiotaBuilder(logger, configuration, _httpClient).GenerateClientAsync(TestContext.Current.CancellationToken);

        // "gofmt -l" lists the files whose formatting differs from gofmt's. The generated code is
        // expected to already be formatted, so the command must not report any file.
        var (exitCode, stdOut, stdErr) = await RunProcessAsync(gofmt, ["-l", outputPath], TestContext.Current.CancellationToken);

        Assert.True(string.IsNullOrEmpty(stdErr), $"gofmt reported errors for '{descriptionFile}':\n{stdErr}");
        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(stdOut),
            $"go fmt would reformat the following generated files for '{descriptionFile}', so kiota did not format them correctly:\n{stdOut}");
    }

    private static string GetGoFmtPath()
    {
        var executableName = OperatingSystem.IsWindows() ? "gofmt.exe" : "gofmt";

        // gofmt ships inside the Go SDK ($GOROOT/bin) which is not always added to PATH on CI agents.
        var goRoot = Environment.GetEnvironmentVariable("GOROOT");
        if (!string.IsNullOrEmpty(goRoot))
        {
            var candidate = Path.Combine(goRoot, "bin", executableName);
            if (File.Exists(candidate)) return candidate;
        }

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVariable))
            foreach (var directory in pathVariable.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory)) continue;
                var candidate = Path.Combine(directory, executableName);
                if (File.Exists(candidate)) return candidate;
            }

        return string.Empty;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(string fileName, string[] arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, await stdOutTask, await stdErrTask);
    }

    private static string GetAbsolutePath(string relativePath) => Path.Combine(Directory.GetCurrentDirectory(), relativePath);
}
