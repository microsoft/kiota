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
  $fragments = $version.Split("-preview.")
  $versionParts = $fragments[0].Split(".")
  $sequenceNumber = $fragments[1].Substring(8).TrimStart("0")
  if ($sequenceNumber.Length -eq 1) {
    $sequenceNumber = "0$sequenceNumber"
  }
  # regardless of the release channel, the highest version is the one that will be used
  # for that reason, we make sure we have the same number of digits for patch versions regardless of the release channel
  # and we max out the lower weight digits for the release version
  # the new patch version will be following this format ppyyMMddss
  # where
  #   pp is the patch version without heading zeros
  #   yyMMdd is the current date
  #   ss is the sequence number from ADO build arguably that maxes us to 99 previews per day
  $extensionVersion = $versionParts[0] + "." + $versionParts[1] + "." + $versionParts[2].TrimStart("0") + (Get-Date).ToString("yyMMdd") + $sequenceNumber
}
else {
  $versionParts = $version.Split(".")
  $patchVersion = $versionParts[2].TrimStart("0")
  if ([string]::IsNullOrWhiteSpace($patchVersion)) {
    $patchVersion = "9"
  }
  $extensionVersion = $versionParts[0] + "." + $versionParts[1] + "." + $patchVersion + "99999999"
}
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