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

Instead of publishing, you could also build the project and use the "-Dev" switch that executes Kiota from "src\kiota\bin\Debug\net8.0":

```bash
./it/generate-code.ps1 -descriptionUrl ${FILE/URL} -language ${LANG} -Dev
```

And finally run the test:

```bash
./it/exec-cmd.ps1 -descriptionUrl ${FILE/URL} -language ${LANG}
```

# Suppressions

Some API files do not work for a specific target language. There are two ways to define suppressions:

## Suppress the full test

This will execute the test in Github CI anyway and maybe an error will happen, but the test will not be marked as "failed" (see "integration-tests.yml" and "get-is-suppressed.ps1")

```
  "https://www.sample.com/sample-api.json": {
    "MockServerITFolder": "sampleapi",
    "Suppressions": [
      {
        "Language": "ruby",
        "Rationale": "Feature x in file is not supported for ruby."
      }
    ]
  }
```
 
## Suppress creating certain API paths

This will tell Kiota not to generate the code for the specified endpoint. Use it if something inside this endpoint definition is either invalid OpenAPI
or Kiota cannot handle it.

```
  "https://www.sample.com/sample-api.json": {
    "MockServerITFolder": "sampleapi",
    "ExcludePatterns": [
      {
        "Pattern": "/users/*/gpg_keys",
        "Rationale": "invalid data type for argument XYZ"
      }
      {
        "Pattern": "/repos/{owner}/{repo}/releases#POST",
        "Rationale": "here is something wrong, too"
      }
    ]
  }
```

The second snippet demonstrates that you can suppress also creating a method for just one of the HTTP methods of an endpoint.

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
