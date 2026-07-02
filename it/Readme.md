# Running the Integration Tests

Run the following steps to locally run the integration tests.

Publish locally a development version of `kiota`:

```bash
dotnet publish ./src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -p:PublishReadyToRun=true -o ./publish -f net10.0
```

Generate the code:

```bash
./it/generate-code.ps1 -descriptionUrl ${FILE/URL} -language ${LANG}
```

And finally run the test:

```bash
./it/exec-cmd.ps1 -descriptionUrl ${FILE/URL} -language ${LANG}
```

# Surface area / DOM diff test

The surface area test guards against **binary/source breaking changes** that a change to
kiota's generation logic could introduce into downstream SDKs (notably the Microsoft Graph
SDKs). It compares the public-API surface (`kiota-dom-export.txt`) produced by the currently
published `Microsoft.OpenApi.Kiota` NuGet tool (baseline) against the surface produced by a
locally built kiota (current changeset).

In CI this runs via `.github/workflows/surface-area-tests.yml` and feeds the resulting patch
to the [`microsoftgraph/kiota-dom-export-diff-tool`](https://github.com/microsoftgraph/kiota-dom-export-diff-tool)
`tool` action with `fail-on-removal: true`: any removed surface line fails the check.

To run it locally, first publish a development build of `kiota`:

```bash
dotnet publish ./src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -p:PublishReadyToRun=true -o ./publish -f net10.0
```

Then generate the baseline/current exports and a diff patch:

```bash
./it/compare-dom-export.ps1 -descriptionUrl ${FILE/URL} -language ${LANG}
```

Useful options:

* `-baselineVersion` — pin a specific published `Microsoft.OpenApi.Kiota` version (defaults to latest stable).
* `-patchPath` — where to write the unified diff (defaults to `./<language>-dom-export.patch`).
* `-kiotaExec` — path to the locally built kiota (defaults to `./publish/kiota`).
* `-additionalArguments` — extra arguments forwarded to both `kiota generate` invocations.

**Interpreting results:** removed lines (`-`) in the patch indicate API removed/changed since
the baseline release — i.e. potential breaking changes. Added lines (`+`) are additive and do
not fail the check. Because the baseline is the last published release, the diff reflects every
change since that release, not just the current changeset.

**Intended breaking changes:** review the explanations produced by the diff tool. If a breaking
change is deliberate, it must be acknowledged through normal PR review (and, where configured, a
required-check override) before merging.

# MockServer tests

The OpenAPI description can be published to a mock server, and you can execute tests that call this API.

To do so, first define a "MockServerITFolder" property in "config.json" 

```
  "./tests/Kiota.Builder.IntegrationTests/MySampleAPI.yml": {
    "MockServerITFolder": "mysample"
  },
```

When calling "exec-cmd.ps1" for a specific language, the scripts checks whether this sub folder exists in
the directory corresponding to the language.
If it exists, it executes the tests found in this directory.

The handling depends on the language:

* C#: place the tests in "it\csharp\mysample". Use the test class "basic\KiotaMockServerTests.cs" as a test file template.
  The subdir should also contain a project file and maybe "Usings.cs" if your test class relies on global usings.
  But you can use the default files: if "exec-cmd.ps1" finds no csproj file in the subdir, it copies "basic\basic.csproj" to
  your test subdir and removes it afterwards. The same is done for "Usings.cs"
* Dart: place the tests in "it\dart\mysample\test". Use the test class "it\dart\basic\test\api_client_test.dart" as a test file template.
  No additional files are required.
* Go: place the tests in "it\go\mysample". Use the test class  "it\go\basic\client_test.go" as a test file template.
  The subdir should also contain "go.mod" and "go.sum".
  But you can use the default files: if "exec-cmd.ps1" does not find them in the subdir, it copies them from the "basic" dir to
  your test subdir and removes them afterwards.
* Java: place the tests in "it\java\mysample\src\test\java". Use the test class "it\java\basic\src\test\java\BasicAPITest.java" as a test file template.
  The subdir should also contain "pom.xml".
  But you can use the default file: if "exec-cmd.ps1" does not find it in the subdir, it copies it from the "basic" dir to
  your test subdir and removes it afterwards.
* PHP: place the tests in "it\php\mysample\tests". Use the test class "it\php\basic\tests\SampleTest.php" as a test file template.
  The subdir should also contain "composer.json" and "phpstan.neon".
  But you can use the default files: if "exec-cmd.ps1" does not find them in the subdir, it copies them from the "basic" dir to
  your test subdir and removes them afterwards.
* Python: place the tests in "it\python\mysample". Use the test class "it\python\basic\test_sample.py" as a test file template.
  No additional files are required.
* Ruby: place the tests in "it\ruby\spec\mysample" (difference to other tests!). Use the test class "it\ruby\spec\defaultvalues\integration_test_defaultvalues.rb" as a test file template.
  No additional files are required.
* Typescript: not supported.

If you create e.g. a custom "csproj" file for your test (might be necessary if you need additional dependencies), add this file
to the Dependabot config so that dependencies are updated.
