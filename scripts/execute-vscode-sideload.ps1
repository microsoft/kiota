param (
    [string]
    [Parameter(Mandatory = $false)]
    $Platform = "win-x64",
    [switch]
    $WSL
)


Push-Location ./vscode/packages/microsoft-kiota

# Install all dependencies
npm i

# Package the VS Code extension
npx @vscode/vsce package

# Getting package for its version
$PackageJson = Get-Content "package.json" -Raw | ConvertFrom-Json

# Install the extension
code --install-extension "./kiota-$($PackageJson.version).vsix"

if ($WSL) {
    $Platform = "linux-x64"
    $VsCodeRootPath = (Resolve-Path "~/.vscode-server").Path
}
else {
    $VsCodeRootPath = (Resolve-Path "~/.vscode").Path
}

$KiotaBin = "$VsCodeRootPath/extensions/ms-graph.kiota-$($PackageJson.version)/.kiotabin/$($PackageJson.kiotaVersion)/$Platform"

Pop-Location

# Building Kiota
dotnet publish ./src/kiota/kiota.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained -f net10.0 -c Release -r $platform -o $KiotaBin