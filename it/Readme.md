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
