param (
    [string]
    [Parameter(Mandatory = $true)]
    $version,
    [string]
    [Parameter(Mandatory = $true)]
    $filePath,
    [string]
    [Parameter(Mandatory = $true)]
    $binaryFolderPath
)

$version = $version.TrimStart("v")
$packageJson = Get-Content $filePath | ConvertFrom-Json
$packageJson.version = $version

$runtimeDependencies = $packageJson.runtimeDependencies
foreach ($runtimeDependency in $runtimeDependencies) {
    $runtimeDependency.sha256 = (Get-FileHash "$binaryFolderPath/$($runtimeDependency.platformId).zip" -Algorithm SHA256).Hash
}
Set-Content $filePath ($packageJson | ConvertTo-Json -Depth 10)