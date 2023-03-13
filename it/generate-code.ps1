#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl,
    [Parameter(Mandatory = $true)][string]$language
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
$kiotaExec = Join-Path -Path $rootPath -ChildPath "publish"
$kiotaExec = Join-Path -Path $kiotaExec -ChildPath "kiota"
Start-Process "$kiotaExec" -ArgumentList "generate --language ${language} --openapi ${descriptionUrl}${additionalArguments}" -Wait -NoNewWindow
