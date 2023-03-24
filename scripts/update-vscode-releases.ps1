param (
    [string]
    [Parameter(Mandatory = $true)]
    $version,
    [string]
    [Parameter(Mandatory = $true)]
    $filePath
)

$version = $version.TrimStart("v")
$packageJson = Get-Content $filePath | ConvertFrom-Json
$packageJson.version = $version

$tempFolderPath = Join-Path $Env:Temp "kiota-vscode-$version"
New-Item -Type Directory -Path $tempFolderPath | Out-Null
$runtimeDependencies = $packageJson.runtimeDependencies
foreach ($runtimeDependency in $runtimeDependencies) {
    $runtimeDependency.url = "https://github.com/microsoft/kiota/releases/download/v$version/$($runtimeDependency.platformId).zip"
    Invoke-WebRequest -Uri $runtimeDependency.url -OutFile "$tempFolderPath/$($runtimeDependency.platformId).zip"
    $runtimeDependency.sha256 = (Get-FileHash "$tempFolderPath/$($runtimeDependency.platformId).zip" -Algorithm SHA256).Hash
}
Set-Content $filePath ($packageJson | ConvertTo-Json -Depth 10)

Remove-Item -r $tempFolderPath -Force -ErrorAction SilentlyContinue