# Get the latest version of a package from NuGet
function Get-LatestNugetVersion {
    param(
        [string]$packageId
    )

    $url = "https://api.nuget.org/v3/registration5-gz-semver2/$($packageId.ToLowerInvariant())/index.json"
    $response = Invoke-RestMethod -Uri $url -Method Get
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
    $url = "https://registry.npmjs.org/$($packageId.ToLowerInvariant())/latest"
    $response = Invoke-RestMethod -Uri $url -Method Get
    $response.version
}
# Get the latest version of a maven package
function Get-LatestMavenVersion {
    param(
        [string]$packageId
    )
    $url = "https://repo1.maven.org/maven2/$($packageId.Replace(":", "/").Replace(".", "/").Replace("|", "."))/maven-metadata.xml"
    $response = Invoke-RestMethod -Uri $url -Method Get
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
    $url = "https://pypi.org/pypi/$($packageId.ToLowerInvariant())/json"
    $response = Invoke-RestMethod -Uri $url -Method Get
    $response.info.version
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
    else {
        Write-Warning "Unsupported language $languageName you need to update it manually"
    }
}

# Write the updated appsettings.json file
$appSettings | ConvertTo-Json -Depth 100 | Set-Content -Path $mainSettings -NoNewLine

