#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl
)
if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
    exit 1
}
# 97 so we don't exceed the 128 character limit for artifact names with the generation-results-typescript prefix
$replaced = ($descriptionUrl -replace "[-:<>|\*\?\\\/\.]", "_").Substring(0, [Math]::Min(97, $descriptionUrl.Length))
Write-Output "ARTKEY=$replaced" >> $Env:GITHUB_OUTPUT

if ($descriptionUrl.StartsWith("./")) {
    Write-Output "DESCRIPTION_PATH=$descriptionUrl" >> $Env:GITHUB_OUTPUT
}
else {
    Write-Output "DESCRIPTION_PATH=./description/openapi.yaml" >> $Env:GITHUB_OUTPUT
}