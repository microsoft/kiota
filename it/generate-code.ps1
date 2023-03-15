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

switch ($dev)
{
    $true {
        Write-Warning "Using kiota in dev mode"
        $kiotaExec = Join-Path -Path $rootPath -ChildPath "src"
        $kiotaExec = Join-Path -Path $kiotaExec -ChildPath "kiota"
        $kiotaExec = Join-Path -Path $kiotaExec -ChildPath "bin"
        $kiotaExec = Join-Path -Path $kiotaExec -ChildPath "Debug"
        $kiotaExec = Join-Path -Path $kiotaExec -ChildPath "net7.0"
        $kiotaExec = Join-Path -Path $kiotaExec -ChildPath "kiota"
        break
    }
    default { 
        $kiotaExec = Join-Path -Path $rootPath -ChildPath "publish"
        $kiotaExec = Join-Path -Path $kiotaExec -ChildPath "kiota"
        break
    }
}
Start-Process "$kiotaExec" -ArgumentList "generate --clean-output --language ${language} --openapi ${descriptionUrl}${additionalArguments}" -Wait -NoNewWindow
