# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

<#
.SYNOPSIS
Checks whether a NuGet artifact's version exists in an authenticated Azure Artifacts feed.
.DESCRIPTION
Resolves the package content endpoint from the private feed's NuGet v3 service index.
Sets nugetAlreadyPublished for the ESRP release steps; only a missing package or version
permits publishing. Feed authentication and other lookup failures fail the step.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,
    [Parameter(Mandatory = $true)]
    [string]$PackageId,
    [Parameter(Mandatory = $true)]
    [string]$NuGetServiceIndexUrl,
    [string]$FeedAccessToken = $env:FEED_ACCESS_TOKEN
)

$ErrorActionPreference = 'Stop'

function Assert-PrivateFeedUrl {
    param([string]$Url)

    $uri = [uri]$Url
    if (-not $uri.IsAbsoluteUri -or $uri.Scheme -ne 'https' -or
        ($uri.Host -ne 'pkgs.dev.azure.com' -and -not $uri.Host.EndsWith('.pkgs.visualstudio.com'))) {
        throw "NuGet lookups must use an HTTPS Azure Artifacts feed: $Url"
    }
}

Assert-PrivateFeedUrl -Url $NuGetServiceIndexUrl
if ([string]::IsNullOrWhiteSpace($FeedAccessToken)) {
    throw 'FEED_ACCESS_TOKEN is required to query the private NuGet feed.'
}

$packagePattern = '^' + [regex]::Escape($PackageId) + '\.(\d[\w\.\-]*)\.nupkg$'
$packages = @(Get-ChildItem -Path $PackageDirectory -File -Filter "$PackageId.*.nupkg" |
    Where-Object { $_.Name -match $packagePattern })
if ($packages.Count -ne 1) {
    throw "Expected exactly one $PackageId nupkg to publish; found $($packages.Count)."
}
$version = [regex]::Match($packages[0].Name, $packagePattern, 'IgnoreCase').Groups[1].Value
$id = $PackageId.ToLowerInvariant()
$credentials = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("AzureDevOps:$FeedAccessToken"))
$headers = @{
    'Authorization' = "Basic $credentials"
    'User-Agent' = 'openapi-azdo-pipeline'
}

$index = Invoke-RestMethod -Uri $NuGetServiceIndexUrl -Headers $headers -MaximumRedirection 0
$resource = $index.resources | Where-Object { $_.'@type' -eq 'PackageBaseAddress/3.0.0' } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($resource.'@id')) {
    throw "No PackageBaseAddress resource found in the NuGet service index at $NuGetServiceIndexUrl"
}
$uri = "$($resource.'@id'.TrimEnd('/'))/$id/index.json"
Assert-PrivateFeedUrl -Url $uri

try {
    $response = Invoke-RestMethod -Uri $uri -Headers $headers -MaximumRedirection 0
    if ($null -eq $response.versions) {
        throw "No versions returned for NuGet $id by the private feed."
    }
    $alreadyPublished = $response.versions -contains $version
}
catch {
    if ([int]$_.Exception.Response.StatusCode -eq 404) {
        $alreadyPublished = $false
    }
    else {
        throw
    }
}

if ($alreadyPublished) {
    Write-Host "NuGet $id $version already present in the private feed; skipping ESRP release (idempotent re-run)."
}
else {
    Write-Host "NuGet $id $version not found in the private feed; will publish via ESRP."
}
Write-Host "##vso[task.setvariable variable=nugetAlreadyPublished]$($alreadyPublished.ToString().ToLowerInvariant())"
