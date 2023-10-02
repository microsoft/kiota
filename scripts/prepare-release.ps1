param (
    [string]
    $version,
    [string]
    $date
)

$indentContentScriptPath = Join-Path -Path $PSScriptRoot -ChildPath "indent-xml-content.ps1"
. $indentContentScriptPath

if ($date -eq $null -or $date -eq "") {
    $date = Get-Date -Format "yyyy-MM-dd"
    Write-Information "No date provided, using $date"
}

$mainCSProjpath = Join-Path -Path $PSScriptRoot -ChildPath "../src/kiota/kiota.csproj"
$mainCsprojContent = [xml](Get-Content -Path $mainCSProjpath)
if ($version -eq $null -or $version -eq "") {
    $version = $mainCsprojContent.Project.PropertyGroup[0].VersionPrefix
    Write-Information "No version provided, using $version"
}
else {
    if ($version -notmatch "^\d+\.\d+\.\d+$") {
        throw "Version $version is not a valid version, please use the format x.y.z"
    }
    $mainCsprojContent.Project.PropertyGroup[0].VersionPrefix = $version
    Format-XMLIndent -Content $mainCsprojContent | Set-Content -Path $mainCSProjpath -Encoding utf8
}
$builderCSProjPath = Join-Path -Path $PSScriptRoot -ChildPath "../src/Kiota.Builder/Kiota.Builder.csproj"
$builderCsprojContent = [xml](Get-Content -Path $builderCSProjPath)
$builderCsprojContent.Project.PropertyGroup[0].VersionPrefix = $version
Format-XMLIndent -Content $builderCsprojContent | Set-Content -Path $builderCSProjPath -Encoding utf8

$checkScriptPath = Join-Path -Path $PSScriptRoot -ChildPath "get-release-notes.ps1"

# checks we have unreleased changes
& $checkScriptPath -version "Unreleased"

# checks this version is not already released
$changelogPath = Join-Path -Path $PSScriptRoot -ChildPath "../CHANGELOG.md"
$changeLogContent = Get-Content $changelogPath -Raw

if ($changeLogContent.Contains("## [$version]")) {
    throw "Version $version already released, please bump the version"
}

# updates the changelog

$splatReleaseNotes = $changeLogContent.Split("## [Unreleased]")
$changeLogContent = $splatReleaseNotes[0] + "## [Unreleased]`n`n### Added`n`n### Changed`n`n## [$version] - $date" + $splatReleaseNotes[1]
Set-Content -Path $changelogPath -Value $changeLogContent



