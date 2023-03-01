param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl
)

if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
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

$command = "";

if ($descriptionValue.PSObject.Properties.Name -contains "ExcludePatterns") {
    $descriptionValue.ExcludePatterns | ForEach-Object {
        $command += " -e $($_.Pattern)"
    }
}

if ($descriptionValue.PSObject.Properties.Name -contains "IncludePatterns") {
    $descriptionValue.IncludePatterns | ForEach-Object {
        $command += " -i $($_.Pattern)"
    }
}

return $command
