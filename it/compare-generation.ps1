#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl,
    [Parameter(Mandatory = $true)][string]$language,
    [Parameter(Mandatory = $false)][switch]$dev,
    [Parameter(Mandatory = $false)][switch]$preserveOutput
)

if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
    exit 1
}

if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}

function New-TemporaryDirectory {
    $parent = [System.IO.Path]::GetTempPath()
    [string] $name = [System.Guid]::NewGuid()
    New-Item -ItemType Directory -Path (Join-Path $parent $name)
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootPath = Join-Path -Path $scriptPath -ChildPath ".."

$Env:KIOTA_TUTORIAL_ENABLED = "false"
$executableName = "kiota"
if ($IsWindows) {
    $executableName = "kiota.exe"
}

switch ($dev) {
    $true {
        Write-Warning "Using kiota in dev mode"
        $kiotaExec = Join-Path -Path $rootPath -ChildPath "src" -AdditionalChildPath "kiota", "bin", "Debug", "net7.0", $executableName
        break
    }
    default { 
        $kiotaExec = Join-Path -Path $rootPath -ChildPath "publish" -AdditionalChildPath $executableName
        break
    }
}

$targetOpenapiPath = Join-Path -Path $scriptPath -ChildPath "openapi.yaml"
if (Test-Path $targetOpenapiPath) {
    Remove-Item $targetOpenapiPath
}

if ($descriptionUrl.StartsWith("./")) {
    Copy-Item -Path $descriptionUrl -Destination $targetOpenapiPath -Force
}
elseif ($descriptionUrl.StartsWith("http")) {
    Invoke-WebRequest -Uri $descriptionUrl -OutFile $targetOpenapiPath
}
else {
    Start-Process "$kiotaExec" -ArgumentList "download ${descriptionUrl} --clean-output --output $targetOpenapiPath" -Wait -NoNewWindow    
}

$tmpFolder1 = New-TemporaryDirectory
$tmpFolder2 = New-TemporaryDirectory

Start-Process "$kiotaExec" -ArgumentList "generate --clean-output --language ${language} --openapi ${targetOpenapiPath} --dvr all --output $tmpFolder1" -Wait -NoNewWindow
Start-Process "$kiotaExec" -ArgumentList "generate --clean-output --language ${language} --openapi ${targetOpenapiPath} --dvr all --output $tmpFolder2" -Wait -NoNewWindow

# Remove variable output files
Remove-Item (Join-Path -Path $tmpFolder1 -ChildPath "kiota-lock.json")
if (Test-Path (Join-Path -Path $tmpFolder1 -ChildPath ".kiota.log")) {
    Remove-Item -Force (Join-Path -Path $tmpFolder1 -ChildPath ".kiota.log")
}
Remove-Item (Join-Path -Path $tmpFolder2 -ChildPath "kiota-lock.json")
if (Test-Path (Join-Path -Path $tmpFolder2 -ChildPath ".kiota.log")) {
    Remove-Item -Force (Join-Path -Path $tmpFolder2 -ChildPath ".kiota.log")
}

# Compare hashes
$HashString1 = (Get-ChildItem $tmpFolder1 -Recurse | where { ! $_.PSIsContainer } | Get-FileHash -Algorithm MD5).Hash | Out-String
Get-FileHash -InputStream ([IO.MemoryStream]::new([char[]]$HashString1))

$HashString2 = (Get-ChildItem $tmpFolder2 -Recurse | where { ! $_.PSIsContainer } | Get-FileHash -Algorithm MD5).Hash | Out-String
Get-FileHash -InputStream ([IO.MemoryStream]::new([char[]]$HashString2))

Write-Output "Folder 1: $tmpFolder1"
Write-Output "Folder 2: $tmpFolder2"

if ($HashString1 -eq $HashString2) {
    Write-Output "The content of the folders is identical"

    if (!$preserveOutput) {
        Remove-Item $tmpFolder1 -Force -Recurse
        Remove-Item $tmpFolder2 -Force -Recurse
    }

    Exit 0
}
else {
    Write-Error "The content of the folders is NOT identical"
    $archivePath1 = Join-Path $rootPath -ChildPath "idempotency-folder1.zip"
    $archivePath2 = Join-Path $rootPath -ChildPath "idempotency-folder2.zip"

    if (Test-Path $archivePath1) {
        Remove-Item $archivePath1 -Force
    }
    if (Test-Path $archivePath2) {
        Remove-Item $archivePath2 -Force
    }

    if ($dev -eq $false) {
        Compress-Archive -Path $tmpFolder1 -DestinationPath $archivePath1
        Compress-Archive -Path $tmpFolder1 -DestinationPath $archivePath2
        Exit 1
    }
}
