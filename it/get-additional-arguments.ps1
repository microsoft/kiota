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
$configPath = Join-Path -Path $scriptPath -ChildPath "config.json"

$jsonValue = Get-Content -Path $configPath -Raw | ConvertFrom-Json

$descriptionValue = $jsonValue.psobject.properties.Where({ $_.name -eq $descriptionUrl }).value
if ($null -eq $descriptionValue) {
    Write-Information "No configuration found for $descriptionUrl"
    exit 0
}

$command = "--output ./it/$language";
if ($language -eq "csharp") {
    $command += "/client"
}

if ($descriptionValue.PSObject.Properties.Name -contains "ExcludePatterns") {
    $descriptionValue.ExcludePatterns | ForEach-Object {
        $command += " -e '$($_.Pattern)'"
    }
}

if ($descriptionValue.PSObject.Properties.Name -contains "IncludePatterns") {
    $descriptionValue.IncludePatterns | ForEach-Object {
        $command += " -i '$($_.Pattern)'"
    }
}

if ($language -eq "csharp") {
    $command += " --namespace-name 'app.first'"
}

return $command
