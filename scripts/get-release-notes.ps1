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
Write-Host "##vso[task.setvariable variable=releaseNotes; isSecret=false; isOutput=true]$releaseNotes"