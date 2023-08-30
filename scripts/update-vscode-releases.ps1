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
$fragments = $version.Split("-preview.")
$versionParts = $fragments[0].Split(".")
$updatedPatchVersion = $versionParts[2].TrimStart("0")
if ($version -like "*-preview.*") {
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
  $updatedPatchVersion += (Get-Date).ToString("yyMMdd") + $sequenceNumber
}
else {
  if ($updatedPatchVersion -eq "1") {
    $updatedPatchVersion = "100000002"
  }
  elseif ([string]::IsNullOrWhiteSpace($updatedPatchVersion)) {
    $updatedPatchVersion = "100000001"
  }
}
$extensionVersion = $versionParts[0] + "." + $versionParts[1] + "." + $updatedPatchVersion
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