# Debugging the extension

1. run `dotnet publish ./src/kiota/kiota.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained -c Release -r <win-x64|linux-x64|osx-x64> -o ./vscode/microsoft-kiota/.kiotabin/<versionInPackage.json>/<rid>/`
1. in `vscode/microsoft-kiota` run `code . && npm i`
1. in the code instance that was opened at step 2, just press f5
