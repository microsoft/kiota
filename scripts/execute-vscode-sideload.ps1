param (
    [string]
    [Parameter(Mandatory = $false)]
    $Platform = "win-x64",
    [switch]
    $WSL
)


Push-Location ./vscode/microsoft-kiota

# Install all dependencies
npm i

# Package the VS Code extension
npx @vscode/vsce package

# Getting package for its version
$PackageJson = Get-Content "package.json" -Raw | ConvertFrom-Json

# Install the extension
code --install-extension "./kiota-$($PackageJson.version).vsix"

$VsCodeRootPath = "~/.vscode"

if ($WSL) {
    $Platform = "linux-x64"
    $VsCodeRootPath = "~/.vscode-server"
}

$KiotaBin = "$VsCodeRootPath/extensions/ms-graph.kiota-$($PackageJson.version)/.kiotabin/$($PackageJson.kiotaVersion)/$Platform"

Pop-Location

# Building Kiota
dotnet publish ./src/kiota/kiota.csproj -p:PublishSingleFile=true -p:PublishReadyToRun=true --self-contained -c Release -r $platform -o $KiotaBin