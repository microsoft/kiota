[CmdletBinding()]
param(
    # NuGet v3 service index. Point at a private Azure Artifacts feed's nuget/v3/index.json to route through upstreams.
    [string]$NuGetServiceIndexUrl = "https://api.nuget.org/v3/index.json",
    # npm registry base URL. Point at a private feed's npm/registry endpoint to route through upstreams.
    [string]$NpmRegistryUrl = "https://registry.npmjs.org",
    # PyPI PEP 503/691 simple index base URL. Point at a private feed's pypi/simple endpoint to route through upstreams.
    [string]$PyPiSimpleIndexUrl = "https://pypi.org/simple",
    # Maven repository base URL. Point at a private feed's maven/v1 endpoint to route through upstreams.
    [string]$MavenRepositoryUrl = "https://repo1.maven.org/maven2",
    # Access token used to authenticate to the private feed (e.g. the pipeline System.AccessToken).
    # Leave empty for anonymous access to the public registries.
    [string]$FeedAccessToken = $env:FEED_ACCESS_TOKEN
)

# Normalize trailing slashes so URL composition is predictable.
$NpmRegistryUrl = $NpmRegistryUrl.TrimEnd('/')
$PyPiSimpleIndexUrl = $PyPiSimpleIndexUrl.TrimEnd('/')
$MavenRepositoryUrl = $MavenRepositoryUrl.TrimEnd('/')

# Build the auth headers once. Azure Artifacts protocol endpoints accept the pipeline OAuth token as a Bearer token.
$script:FeedAuthHeaders = @{}
if (-not [string]::IsNullOrWhiteSpace($FeedAccessToken)) {
    $script:FeedAuthHeaders["Authorization"] = "Bearer $FeedAccessToken"
}

# Cache for the resolved NuGet registrations base URL so the service index is only read once.
$script:NuGetRegistrationsBaseUrl = $null

# Resolve the NuGet registrations base URL from the service index. Prefers the gzipped SemVer 2.0.0
# resource, matching the previously hard-coded registration5-gz-semver2 endpoint, and works against a
# private Azure Artifacts feed as well as the public api.nuget.org index.
function Get-NugetRegistrationsBaseUrl {
    if ($null -ne $script:NuGetRegistrationsBaseUrl) {
        return $script:NuGetRegistrationsBaseUrl
    }
    $index = Invoke-RestMethod -Uri $NuGetServiceIndexUrl -Method Get -Headers $script:FeedAuthHeaders
    $preferredTypes = @("RegistrationsBaseUrl/3.6.0", "RegistrationsBaseUrl/Versioned", "RegistrationsBaseUrl")
    foreach ($type in $preferredTypes) {
        $resource = $index.resources | Where-Object { $_.'@type' -eq $type } | Select-Object -First 1
        if ($null -ne $resource) {
            $script:NuGetRegistrationsBaseUrl = $resource.'@id'.TrimEnd('/')
            return $script:NuGetRegistrationsBaseUrl
        }
    }
    throw "Could not find a RegistrationsBaseUrl resource in the NuGet service index at $NuGetServiceIndexUrl"
}

# Get the latest version of a package from NuGet
function Get-LatestNugetVersion {
    param(
        [string]$packageId
    )

    $registrationsBaseUrl = Get-NugetRegistrationsBaseUrl
    $url = "$registrationsBaseUrl/$($packageId.ToLowerInvariant())/index.json"
    $response = Invoke-RestMethod -Uri $url -Method Get -Headers $script:FeedAuthHeaders
    $version = $response.items | Select-Object -ExpandProperty upper | ForEach-Object { [System.Management.Automation.SemanticVersion]$_ } | sort-object | Select-Object -Last 1
    $version.ToString()
}
# Get the latest github release
function Get-LatestGithubRelease {
    param(
        [string]$packageId
    )
    $packageId = $packageId.Replace("github.com/", "")
    $url = "https://api.github.com/repos/$packageId/releases/latest"
    $response = Invoke-RestMethod -Uri $url -Method Get
    $response.tag_name
}
# Get the latest version of a npm package
function Get-LatestNpmVersion {
    param(
        [string]$packageId
    )
    # Azure Artifacts npm feeds do not implement the /{package}/latest dist-tag shortcut (they return
    # 404 for it). Fetch the full packument at /{package} instead and read dist-tags.latest, which works
    # against both the private feed and public registry.npmjs.org. Scoped names (e.g.
    # @microsoft/kiota-abstractions) must have their '/' encoded as %2F for the packument request.
    $encodedId = $packageId.ToLowerInvariant().Replace('/', '%2F')
    $url = "$NpmRegistryUrl/$encodedId"
    $response = Invoke-RestMethod -Uri $url -Method Get -Headers $script:FeedAuthHeaders
    $response.'dist-tags'.latest
}
# Get the latest version of a maven package
function Get-LatestMavenVersion {
    param(
        [string]$packageId
    )
    $url = "$MavenRepositoryUrl/$($packageId.Replace(":", "/").Replace(".", "/").Replace("|", "."))/maven-metadata.xml"
    $response = Invoke-RestMethod -Uri $url -Method Get -Headers $script:FeedAuthHeaders
    $response.metadata.versioning.latest
}
# Get the latest version of a composer package
function Get-LatestComposerVersion {
    param(
        [string]$packageId
    )
    $url = "https://repo.packagist.org/p2/$($packageId.ToLowerInvariant()).json"
    $response = Invoke-RestMethod -Uri $url -Method Get
    $response.packages.$packageId[0].version
}
# Get the latest version of a pypi package
function Get-LatestPypiVersion {
    param(
        [string]$packageId
    )
    # Use the PEP 503/691 simple index (supported by both public PyPI and Azure Artifacts feeds) instead of
    # the legacy pypi.org "/pypi/{pkg}/json" API, which private Azure Artifacts feeds do not expose.
    $normalizedId = $packageId.ToLowerInvariant().Replace("_", "-").Replace(".", "-")
    $url = "$PyPiSimpleIndexUrl/$normalizedId/"
    $headers = $script:FeedAuthHeaders.Clone()
    $headers["Accept"] = "application/vnd.pypi.simple.v1+json"
    $response = Invoke-RestMethod -Uri $url -Method Get -Headers $headers
    # Prefer the highest final (non pre-release) version to match the previous behaviour of pypi's info.version.
    $stableVersions = $response.versions | Where-Object { $_ -match '^[0-9]+(\.[0-9]+)*$' }
    if ($stableVersions) {
        ($stableVersions | ForEach-Object { [version]$_ } | Sort-Object | Select-Object -Last 1).ToString()
    }
    else {
        $response.versions | Select-Object -Last 1
    }
}
# Get the latest version of a rubygem package
function Get-LatestRubygemVersion {
    param(
        [string]$packageId
    )
    $url = "https://rubygems.org/api/v1/versions/$($packageId.ToLowerInvariant())/latest.json"
    $response = Invoke-RestMethod -Uri $url -Method Get
    $response.version
}
# Get the latest version of a Dart pub.dev package
function Get-LatestPubVersion {
    param(
        [string]$packageId
    )
    $url = "https://pub.dev/api/packages/$($packageId.ToLowerInvariant())"
    $response = Invoke-RestMethod -Uri $url -Method Get -Headers @{ "Accept" = "application/vnd.pub.v2+json" }
    $response.latest.version
}

function Retry-Command {
    [CmdletBinding()]
    Param(
        [Parameter(Position = 0, Mandatory = $true)]
        [scriptblock]$ScriptBlock,

        [Parameter(Position = 1, Mandatory = $false)]
        [int]$Maximum = 5,

        [Parameter(Position = 2, Mandatory = $false)]
        [int]$Delay = 2000
    )

    Begin {
        $cnt = 0
    }

    Process {
        do {
            $cnt++
            try {
                # If you want messages from the ScriptBlock
                # Invoke-Command -Command $ScriptBlock
                # Otherwise use this command which won't display underlying script messages
                $ScriptBlock.Invoke()
                return
            }
            catch {
                Write-Error $_.Exception.InnerException.Message -ErrorAction Continue
                $random = Get-Random -Minimum 1 -Maximum 10
                Start-Sleep -Milliseconds ($Delay * $cnt * $random)
            }
        } while ($cnt -lt $Maximum)

        # Throw an error after $Maximum unsuccessful invocations. Doesn't need
        # a condition, since the function returns upon successful invocation.
        throw 'Execution failed.'
    }
}

function Get-QueryName {
    Param(
        $dependency
    )
    $dependency.QueryName ?? $dependency.Name
}

# Read the appsettings.json file
$mainSettings = Join-Path -Path $PSScriptRoot -ChildPath "..\src\kiota\appsettings.json"
$appSettings = Get-Content -Path $mainSettings -Raw | ConvertFrom-Json

foreach ($languageName in ($appSettings.Languages | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name | Sort-Object)) {
    $language = $appSettings.Languages.$languageName
    if ($languageName -eq "CSharp" -or $languageName -eq "CLI") {
        foreach ($dependency in $language.Dependencies) {
            if ($null -ne $dependency.MaximumVersion -and $dependency.MaximumVersion -eq $dependency.Version) {
                Write-Information "Skipping $($dependency.Name) as it's already at the maximum version"
                continue
            }
            Retry-Command -ScriptBlock {
                $latestVersion = Get-LatestNugetVersion -packageId (Get-QueryName -dependency $dependency)
                Write-Information "Updating $($dependency.Name) from $($dependency.Version) to $latestVersion"
                $dependency.Version = $latestVersion
            }
        }
    }
    elseif ($languageName -eq "Go") {
        foreach ($dependency in $language.Dependencies) {
            if ($null -ne $dependency.MaximumVersion -and $dependency.MaximumVersion -eq $dependency.Version) {
                Write-Information "Skipping $($dependency.Name) as it's already at the maximum version"
                continue
            }
            Retry-Command -ScriptBlock {
                $latestVersion = Get-LatestGithubRelease -packageId (Get-QueryName -dependency $dependency)
                Write-Information "Updating $($dependency.Name) from $($dependency.Version) to $latestVersion"
                $dependency.Version = $latestVersion
            }
        }
    }
    elseif ($languageName -eq "TypeScript") {
        foreach ($dependency in $language.Dependencies) {
            if ($null -ne $dependency.MaximumVersion -and $dependency.MaximumVersion -eq $dependency.Version) {
                Write-Information "Skipping $($dependency.Name) as it's already at the maximum version"
                continue
            }
            Retry-Command -ScriptBlock {
                $latestVersion = Get-LatestNpmVersion -packageId (Get-QueryName -dependency $dependency)
                Write-Information "Updating $($dependency.Name) from $($dependency.Version) to $latestVersion"
                $dependency.Version = $latestVersion
            }
        }
    }
    elseif ($languageName -eq "Java") {
        foreach ($dependency in $language.Dependencies) {
            if ($null -ne $dependency.MaximumVersion -and $dependency.MaximumVersion -eq $dependency.Version) {
                Write-Information "Skipping $($dependency.Name) as it's already at the maximum version"
                continue
            }
            Retry-Command -ScriptBlock {
                $latestVersion = Get-LatestMavenVersion -packageId (Get-QueryName -dependency $dependency)
                Write-Information "Updating $($dependency.Name) from $($dependency.Version) to $latestVersion"
                $dependency.Version = $latestVersion
            }
        }
    }
    elseif ($languageName -eq "PHP") {
        foreach ($dependency in $language.Dependencies) {
            if ($null -ne $dependency.MaximumVersion -and $dependency.MaximumVersion -eq $dependency.Version) {
                Write-Information "Skipping $($dependency.Name) as it's already at the maximum version"
                continue
            }
            Retry-Command -ScriptBlock {
                $latestVersion = Get-LatestComposerVersion -packageId (Get-QueryName -dependency $dependency)
                Write-Information "Updating $($dependency.Name) from $($dependency.Version) to $latestVersion"
                $dependency.Version = $latestVersion
            }
        }
    }
    elseif ($languageName -eq "Python") {
        foreach ($dependency in $language.Dependencies) {
            if ($null -ne $dependency.MaximumVersion -and $dependency.MaximumVersion -eq $dependency.Version) {
                Write-Information "Skipping $($dependency.Name) as it's already at the maximum version"
                continue
            }
            Retry-Command -ScriptBlock {
                $latestVersion = Get-LatestPypiVersion -packageId (Get-QueryName -dependency $dependency)
                Write-Information "Updating $($dependency.Name) from $($dependency.Version) to $latestVersion"
                $dependency.Version = $latestVersion
            }
        }
    }
    elseif ($languageName -eq "Ruby") {
        foreach ($dependency in $language.Dependencies) {
            if ($null -ne $dependency.MaximumVersion -and $dependency.MaximumVersion -eq $dependency.Version) {
                Write-Information "Skipping $($dependency.Name) as it's already at the maximum version"
                continue
            }
            Retry-Command -ScriptBlock {
                $latestVersion = Get-LatestRubygemVersion -packageId (Get-QueryName -dependency $dependency)
                Write-Information "Updating $($dependency.Name) from $($dependency.Version) to $latestVersion"
                $dependency.Version = $latestVersion
            }
        }
    }
    elseif ($languageName -eq "Dart") {
        foreach ($dependency in $language.Dependencies) {
            if ($null -ne $dependency.MaximumVersion -and $dependency.MaximumVersion -eq $dependency.Version) {
                Write-Information "Skipping $($dependency.Name) as it's already at the maximum version"
                continue
            }
            Retry-Command -ScriptBlock {
                $latestVersion = Get-LatestPubVersion -packageId (Get-QueryName -dependency $dependency)
                Write-Information "Updating $($dependency.Name) from $($dependency.Version) to $latestVersion"
                $dependency.Version = $latestVersion
            }
        }
    }
    else {
        Write-Warning "Unsupported language $languageName you need to update it manually"
    }
}

# Write the updated appsettings.json file
$appSettings | ConvertTo-Json -Depth 100 | Set-Content -Path $mainSettings -NoNewLine

