[CmdletBinding(DefaultParameterSetName = 'Local')]
param (
    [string]
    [Parameter(Mandatory = $true, ParameterSetName = 'Local')]
    [Parameter(Mandatory = $true, ParameterSetName = 'Online')]
    $version,
    [string]
    [Parameter(Mandatory = $true, ParameterSetName = 'Local')]
    [Parameter(Mandatory = $true, ParameterSetName = 'Online')]
    $filePath,
    [string]
    [Parameter(Mandatory = $true, ParameterSetName = 'Local')]
    [Parameter(Mandatory = $false, ParameterSetName = 'Online')]
    $binaryFolderPath = "",
    [switch]
    [Parameter(Mandatory = $false, ParameterSetName = 'Online')]
    $online
)

$version = $version.TrimStart("v")
$packageJson = Get-Content $filePath | ConvertFrom-Json
$packageJson.kiotaVersion = $version
$extensionVersion = $version
if ($version -like "*-preview.*") {
    $extensionVersion = $version.Replace("-preview.", "1")
}
else {
    $extensionVersion = $version + "1000000000000"
}
$extensionVersionSegments = $extensionVersion.Split(".")
$extensionVersion = $extensionVersionSegments[0] + "." + $extensionVersionSegments[1] + "." + $extensionVersionSegments[2].TrimStart("0")
$packageJson.version = $extensionVersion
$runtimeDependencies = $packageJson.runtimeDependencies

if ($online) {
    Write-Warning "Downloading binaries from GitHub."
    $binaryFolderPath = Join-Path ($Env:TEMP ?? $PWD) ".kiota-vscode-$version" # $Env:TEMP is not available on GitHub Actions
    New-Item -ItemType Directory -Force -Path $binaryFolderPath
    foreach ($runtimeDependency in $runtimeDependencies) {
        try {
            $url = "https://github.com/microsoft/kiota/releases/download/v$version/$($runtimeDependency.platformId).zip"
            Invoke-WebRequest -Uri $url -OutFile "$binaryFolderPath/$($runtimeDependency.platformId).zip"
        }
        catch {
            Write-Warning "Could not download $url"
        }
    }
}

foreach ($runtimeDependency in $runtimeDependencies) {
    $binPath = Join-Path -Path $binaryFolderPath -ChildPath "$($runtimeDependency.platformId).zip"
    if (Test-Path $binPath) {
        $runtimeDependency.sha256 = (Get-FileHash $binPath -Algorithm SHA256).Hash
    }
    else {
        $runtimeDependency.sha256 = "placeholder"
        Write-Warning "Could not find file $binPath"
    }
}
Set-Content $filePath ($packageJson | ConvertTo-Json -Depth 10)
if ($online) {
    Remove-Item -Recurse -Force $binaryFolderPath
}