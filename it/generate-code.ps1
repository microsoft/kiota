#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl,
    [Parameter(Mandatory = $true)][string]$language,
    [Parameter(Mandatory = $false)][switch]$dev
)

if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
    exit 1
}

if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}

$additionalArgumentCmd = Join-Path -Path $PSScriptRoot -ChildPath "get-additional-arguments.ps1"
$additionalArguments = Invoke-Expression "$additionalArgumentCmd -descriptionUrl $descriptionUrl -language $language"
$rootPath = Join-Path -Path $PSScriptRoot -ChildPath ".."

$Env:KIOTA_TUTORIAL_ENABLED = "false"
$executableName = "kiota"
if ($IsWindows) {
    $executableName = "kiota.exe"
}

switch ($dev) {
    $true {
        Write-Warning "Using kiota in dev mode"
        $kiotaExec = Join-Path -Path $rootPath -ChildPath "src" -AdditionalChildPath "kiota", "bin", "Debug", "net8.0", $executableName
        break
    }
    default { 
        $kiotaExec = Join-Path -Path $rootPath -ChildPath "publish" -AdditionalChildPath $executableName
        break
    }
}

$targetOpenapiPath = Join-Path -Path $PSScriptRoot -ChildPath "openapi.yaml"
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
    $downloadProcess = Start-Process "$kiotaExec" -ArgumentList "download ${descriptionUrl} --clean-output --output $targetOpenapiPath" -Wait -NoNewWindow -PassThru
    if ($downloadProcess.ExitCode -ne 0) {
        Write-Error "Failed to download the openapi description"
        exit 1
    }
}

$generationProcess = Start-Process "$kiotaExec" -ArgumentList "generate --exclude-backward-compatible --clean-output --language ${language} --openapi ${targetOpenapiPath}${additionalArguments}" -Wait -NoNewWindow -PassThru

if ($generationProcess.ExitCode -ne 0) {
    Write-Error "Failed to generate the code for ${language}"
    exit 1
}