param (
    [string]
    [Parameter(Mandatory = $true)]
    $version,
    [string]
    [Parameter(Mandatory = $true)]
    $filePath
)

$version = $version.TrimStart("v")

$changeLogContent = Get-Content $filePath -Raw
$headerLine = "## [$version]"
$headerLineNumber = $changeLogContent.IndexOf("`n" , $changeLogContent.IndexOf($headerLine)) + 3
$endLineNumber = $changeLogContent.IndexOf("## [", $headerLineNumber) - 3
$releaseNotes = $changeLogContent.Substring($headerLineNumber, $endLineNumber - $headerLineNumber)
$targetFile = Join-Path (Split-Path $filePath -Parent) "release-notes.txt"
Set-Content -Path $targetFile -Value $releaseNotes -Verbose