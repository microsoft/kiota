#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl,
    [Parameter(Mandatory = $true)][string]$language,
    [string]$kind = "integration"
)

if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
    exit 1
}

if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}

$configPath = Join-Path -Path $PSScriptRoot -ChildPath "config.json"
$jsonValue = Get-Content -Path $configPath -Raw | ConvertFrom-Json
$descriptionValue = $jsonValue.psobject.properties.Where({ $_.name -eq $descriptionUrl }).value

if ($null -ne $descriptionValue) {
    if ($kind -eq "integration" -and $descriptionValue.PSObject.Properties.Name -contains "Suppressions") {
        $languageInformation = $descriptionValue.Suppressions | Where-Object { $_.Language -eq $language -or $_.Language -eq "all" } | Select-Object -First 1
        if ($null -ne $languageInformation) {
            Write-Warning "Suppressed $descriptionUrl for $language, rationale: $($languageInformation.Rationale)"
            return $true
        }
    }
    elseif ($kind -eq "idempotency" -and $descriptionValue.PSObject.Properties.Name -contains "IdempotencySuppressions") {
        $languageInformation = $descriptionValue.IdempotencySuppressions | Where-Object { $_.Language -eq $language -or $_.Language -eq "all" } | Select-Object -First 1
        if ($null -ne $languageInformation) {
            Write-Warning "Suppressed $descriptionUrl for $language, rationale: $($languageInformation.Rationale)"
            return $true
        }
    }
}

Write-Information "No suppression found for $descriptionUrl for $language"

return $false