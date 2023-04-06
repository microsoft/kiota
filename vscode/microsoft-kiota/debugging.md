# Debugging the extension

1. in `src/kiota` run `dotnet watch publish -p:PublishSingleFile=true -p:PublishReadyToRun=true -c Release -r <win-x64|linux-x64|osx-x64> -o <pathToThisFolder>\.kiotabin\<versionInPackage.json>\<rid>\`
1. in `vscode/microsoft-kiota` run `code . && npm i`
1. in the code instance that was opened at step 2, just press f5
