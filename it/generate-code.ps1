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

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$additionalArgumentCmd = Join-Path -Path $scriptPath -ChildPath "get-additional-arguments.ps1"
$additionalArguments = Invoke-Expression "$additionalArgumentCmd -descriptionUrl $descriptionUrl -language $language"
$rootPath = Join-Path -Path $scriptPath -ChildPath ".."

$executableName = "kiota"
if ($IsWindows) {
    $executableName = "kiota.exe"
}

switch ($dev)
{
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
if (!$descriptionUrl.StartsWith("./")) {
    Start-Process "$kiotaExec" -ArgumentList "download ${descriptionUrl} --clean-output --output $targetOpenapiPath" -Wait -NoNewWindow    
} else if (!$descriptionUrl.StartsWith("http")) {
    Invoke-WebRequest -Uri $descriptionUrl -OutFile "C:\path\file"
} else {
    Copy-Item -Path $descriptionUrl -Destination $targetOpenapiPath
}

Start-Process "$kiotaExec" -ArgumentList "generate --clean-output --language ${language} --openapi ${targetOpenapiPath}${additionalArguments}" -Wait -NoNewWindow
