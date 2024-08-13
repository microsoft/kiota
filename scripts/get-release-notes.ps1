param (
    [string]
    [Parameter(Mandatory = $true)]
    $version,
    [switch]
    [bool]
    $createNotes
)

$version = $version.TrimStart("v")

$changelogPath = Join-Path -Path $PSScriptRoot -ChildPath "../CHANGELOG.md"

$changeLogContent = Get-Content $changelogPath -Raw
$headerLine = "## [$version]"
$versionLineNumber = $changeLogContent.IndexOf($headerLine)
if ($version -ne "Unreleased" -and $versionLineNumber -lt 0) {
    throw "Version $version not found in CHANGELOG.md make sure you've updated the changelog"
}
$headerLineNumber = $changeLogContent.IndexOf("`n" , $versionLineNumber) + 3
$endLineNumber = $changeLogContent.IndexOf("## [", $headerLineNumber) - 3
$releaseNotes = $changeLogContent.Substring($headerLineNumber, $endLineNumber - $headerLineNumber)
if ($version -eq "Unreleased" -and [string]::IsNullOrWhiteSpace($releaseNotes.Replace("### Changed", "").Replace("### Added", ""))) {
    throw "Version $version not found in CHANGELOG.md make sure you've updated the changelog"
}
if ($createNotes -eq $true) {
    $targetFile = Join-Path (Split-Path $changelogPath -Parent) "release-notes.txt"
    Set-Content -Path $targetFile -Value $releaseNotes -Verbose
}