# Running the Integration Tests

Run the following steps to locally run the integration tests.

Publish locally a development version of `kiota`:

```bash
dotnet publish ./src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -p:PublishReadyToRun=true -o ./publish
```

Generate the code:

```bash
./it/generate-code.ps1 -descriptionUrl ${FILE/URL} -language ${LANG}
```

And finally run the test:

```bash
./it/exec-cmd.ps1 -language ${LANG}
```
