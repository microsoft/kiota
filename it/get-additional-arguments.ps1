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

$command = " --output './it/$language'";
if ($language -eq "csharp") {
    $command = " --output './it/$language/client' --namespace-name 'app.client'"
}
elseif ($language -eq "java") {
    $command = " --output './it/$language/src'"
}
elseif ($language -eq "go") {
    $command = " --output './it/$language/client' --namespace-name 'integrationtest/client'"
}
elseif ($language -eq "typescript") {
    $command = " --output './it/$language/src/app/client' --namespace-name 'app.client'"
}
elseif ($language -eq "ruby") {
    $command = " --output './it/$language/lib/integration_test/client' --namespace-name 'integration_test.client'"
}
elseif ($language -eq "php") {
    $command = " --output './it/$language/src/client' --namespace-name 'integration.test.client'"
}
elseif ($language -eq "python") {
    $command = " --output './it/$language/integration_test/client' --namespace-name 'integration_test.client'"
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$configPath = Join-Path -Path $scriptPath -ChildPath "config.json"
$jsonValue = Get-Content -Path $configPath -Raw | ConvertFrom-Json
$descriptionValue = $jsonValue.psobject.properties.Where({ $_.name -eq $descriptionUrl }).value
if ($null -ne $descriptionValue) {
    if ($descriptionValue.PSObject.Properties.Name -contains "ExcludePatterns") {
        $descriptionValue.ExcludePatterns | ForEach-Object {
            Write-Information "Excluding $($_.Pattern) rationale: $($_.Rationale)"
            $command += " -e '$($_.Pattern)'"
        }
    }

    if ($descriptionValue.PSObject.Properties.Name -contains "IncludePatterns") {
        $descriptionValue.IncludePatterns | ForEach-Object {
            Write-Information "Including $($_.Pattern) rationale: $($_.Rationale)"
            $command += " -i '$($_.Pattern)'"
        }
    }
}
else {
    Write-Information "No configuration found for $descriptionUrl"
}

return $command
