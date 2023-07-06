Write-Warning "This script only updates dotnet dependencies. Other dependencies must be updated manually. As well as the version in the csprojs. You're welcome to augment it."

# Get the latest version of a package from NuGet
function Get-LatestVersion {
    param(
        [string]$packageId
    )

    $url = "https://api.nuget.org/v3/registration5-gz-semver2/$($packageId.ToLowerInvariant())/index.json"
    $response = Invoke-RestMethod -Uri $url -Method Get
    $response.items[0].upper
}

# Get current script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Read the appsettings.json file
$mainSettings = Join-Path -Path $scriptPath -ChildPath "..\src\kiota\appsettings.json"
$appSettings = Get-Content -Path $mainSettings -Raw | ConvertFrom-Json

foreach ($languageName in ($appSettings.Languages | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name | Sort-Object)) {
    $language = $appSettings.Languages.$languageName
    if ($languageName -eq "CSharp" -or $languageName -eq "Shell") {
        foreach ($dependency in $language.Dependencies) {
            $latestVersion = Get-LatestVersion -packageId $dependency.Name
            Write-Information "Updating $dependency.PackageId from $dependency.Version to $latestVersion"
            $dependency.Version = $latestVersion
        }
    }
}

# Write the updated appsettings.json file
$appSettings | ConvertTo-Json -Depth 100 | Set-Content -Path $mainSettings
$additionalSettingsPath = Join-Path -Path $scriptPath -ChildPath "..\src\Kiota.Web\wwwroot\appsettings.json"
$appSettings | ConvertTo-Json -Depth 100 | Set-Content -Path $additionalSettingsPath

