#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl
)
if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
    exit 1
}
$replaced = ($original -replace "[-:<>|\*\?\\\/\.]", "_").Substring(0, [Math]::Min(97, $descriptionUrl.Length))
Write-Output "ARTKEY=$replaced" >> $Env:GITHUB_OUTPUT

if ($descriptionUrl.StartsWith("./")) {
    Write-Output "DESCRIPTION_PATH=$descriptionUrl" >> $Env:GITHUB_OUTPUT
}
else {
    Write-Output "DESCRIPTION_PATH=./description/openapi.yaml" >> $Env:GITHUB_OUTPUT
}