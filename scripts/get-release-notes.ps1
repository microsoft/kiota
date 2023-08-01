param (
    [string]
    [Parameter(Mandatory = $true)]
    $version,
    [switch]
    [bool]
    $check
)

$version = $version.TrimStart("v")

# Get current script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

$changelogPath = Join-Path -Path $scriptPath -ChildPath "../CHANGELOG.md"

$changeLogContent = Get-Content $changelogPath -Raw
$headerLine = "## [$version]"
$versionLineNumber = $changeLogContent.IndexOf($headerLine)
$headerLineNumber = $changeLogContent.IndexOf("`n" , $versionLineNumber) + 3
if ($version -ne "Unreleased" -and $versionLineNumber -lt 0) {
    throw "Version $version not found in CHANGELOG.md make sure you've updated the changelog"
}
$endLineNumber = $changeLogContent.IndexOf("## [", $headerLineNumber) - 3
$releaseNotes = $changeLogContent.Substring($headerLineNumber, $endLineNumber - $headerLineNumber)
if ($version -eq "Unreleased" -and [string]::IsNullOrWhiteSpace($releaseNotes.Replace("### Changed", "").Replace("### Added", ""))) {
    throw "Version $version not found in CHANGELOG.md make sure you've updated the changelog"
}
if ($check -eq $false) {
    $targetFile = Join-Path (Split-Path $changelogPath -Parent) "release-notes.txt"
    Set-Content -Path $targetFile -Value $releaseNotes -Verbose
}