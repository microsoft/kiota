param (
    [string]
    [Parameter(Mandatory = $true)]
    $baseChangelogPath,
    [string]
    [Parameter(Mandatory = $true)]
    $headChangelogPath
)

function Get-UnreleasedEntries {
    param (
        [string]
        [Parameter(Mandatory = $true)]
        $changelogPath
    )

    $lines = Get-Content -Path $changelogPath
    $startIndex = -1
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index].Trim() -match "^## \[Unreleased\]\s*$") {
            $startIndex = $index
            break
        }
    }

    if ($startIndex -lt 0) {
        return @()
    }

    $entries = @()
    for ($index = $startIndex + 1; $index -lt $lines.Count; $index++) {
        $line = $lines[$index].Trim()
        if ($line -match "^## \[[^\]]+\]") {
            break
        }
        if ($line -match "^[-*]\s+\S") {
            $entries += ($line -replace "\s+", " ")
        }
    }
    return $entries
}

$baseEntries = @(Get-UnreleasedEntries -changelogPath $baseChangelogPath)
$headEntries = @(Get-UnreleasedEntries -changelogPath $headChangelogPath)

return ($headEntries | Where-Object { $baseEntries -notcontains $_ }).Count -gt 0
