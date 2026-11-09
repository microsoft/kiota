param (
    [string]
    $version = "Unreleased",
    [string]
    $date,
    [string]
    $changelogPath = "",
    [string]
    $entriesPath = ""
)

# Merges the changelog.d entry files into CHANGELOG.md, see changelog.d/README.md

$ErrorActionPreference = "Stop"

if ($changelogPath -eq "") {
    $changelogPath = Join-Path -Path $PSScriptRoot -ChildPath "../CHANGELOG.md"
}
if ($entriesPath -eq "") {
    $entriesPath = Join-Path -Path $PSScriptRoot -ChildPath "../changelog.d"
}
$isRelease = $version -ne "Unreleased"
if ($isRelease -and $version -notmatch "^\d+\.\d+\.\d+$") {
    throw "Version $version is not a valid version, please use the format x.y.z"
}
if ([string]::IsNullOrEmpty($date)) {
    $date = Get-Date -Format "yyyy-MM-dd"
}

$sections = [ordered]@{ "added" = "### Added"; "changed" = "### Changed" }

function Get-AddedTime {
    param ([string] $path)
    # entries are listed in the order they were merged, uncommitted ones last
    $time = git -C (Split-Path -Path $path -Parent) log --diff-filter=A --format=%ct -1 -- (Split-Path -Path $path -Leaf) 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($time)) {
        return [long]::MaxValue
    }
    return [long]$time
}

function Get-EntryFiles {
    $files = @{}
    foreach ($folder in $sections.Keys) {
        $files[$folder] = @()
    }
    if (-not (Test-Path -Path $entriesPath)) {
        return $files
    }
    foreach ($item in Get-ChildItem -Path $entriesPath -Force) {
        if ($item.PSIsContainer -and -not $sections.Contains($item.Name)) {
            throw "Unknown changelog folder $($item.FullName), entries go in changelog.d/added or changelog.d/changed"
        }
        if (-not $item.PSIsContainer -and $item.Name -ne "README.md") {
            throw "Changelog entry $($item.FullName) must be in changelog.d/added or changelog.d/changed"
        }
    }
    foreach ($folder in $sections.Keys) {
        $folderPath = Join-Path -Path $entriesPath -ChildPath $folder
        if (-not (Test-Path -Path $folderPath)) {
            continue
        }
        $items = @(Get-ChildItem -Path $folderPath -File -Force | Where-Object { $_.Name -ne ".gitkeep" })
        $wrongType = @($items | Where-Object { $_.Extension -ne ".md" })
        if ($wrongType.Count -gt 0) {
            throw "Changelog entries must be Markdown files ending in .md: $($wrongType.FullName -join ', ')"
        }
        $files[$folder] = @($items | Sort-Object -Property @{ Expression = { Get-AddedTime -path $_.FullName } }, Name)
    }
    return $files
}

function Get-EntryText {
    param ([string] $path)
    $lines = @(([IO.File]::ReadAllText($path).Trim() -split "\r?\n") | ForEach-Object { $_.TrimEnd() })
    if ($lines.Count -eq 0 -or $lines[0] -eq "") {
        throw "Changelog entry $path is empty"
    }
    if ($lines[0] -notmatch "^[-*] ") {
        $lines[0] = "- " + $lines[0]
    }
    return $lines -join "`n"
}

$content = [IO.File]::ReadAllText($changelogPath)
$lines = $content.Split("`n")
$start = -1
for ($index = 0; $index -lt $lines.Count; $index++) {
    if ($lines[$index].TrimEnd() -match "^## \[Unreleased\]$") {
        $start = $index
        break
    }
}
if ($start -lt 0) {
    throw "No ## [Unreleased] section found in $changelogPath"
}
$end = $lines.Count
for ($index = $start + 1; $index -lt $lines.Count; $index++) {
    if ($lines[$index] -match "^## \[") {
        $end = $index
        break
    }
}
if ($isRelease -and $content.Contains("## [$version]")) {
    throw "Version $version already released, please bump the version"
}

# what is already under ## [Unreleased], keyed by subsection header
$existing = [ordered]@{}
foreach ($header in $sections.Values) {
    $existing[$header] = @()
}
$current = $null
for ($index = $start + 1; $index -lt $end; $index++) {
    $line = $lines[$index].TrimEnd()
    if ($line -match "^### ") {
        $current = $line
        if (-not $existing.Contains($current)) {
            $existing[$current] = @()
        }
    }
    elseif ($line -ne "") {
        if ($null -eq $current) {
            throw "Unexpected line under ## [Unreleased] before any ### section: $line"
        }
        $existing[$current] += $line
    }
}

$files = Get-EntryFiles
foreach ($folder in $sections.Keys) {
    foreach ($file in $files[$folder]) {
        $existing[$sections[$folder]] += Get-EntryText -path $file.FullName
    }
}

$body = @()
foreach ($header in $existing.Keys) {
    $body += $header
    $body += ""
    if ($existing[$header].Count -gt 0) {
        $body += $existing[$header]
        $body += ""
    }
}
$hasEntries = @($existing.Values | Where-Object { $_.Count -gt 0 }).Count -gt 0

if ($isRelease) {
    if (-not $hasEntries) {
        throw "There are no unreleased changes to release, add entries to changelog.d first"
    }
    $section = @("## [Unreleased]", "", "### Added", "", "### Changed", "", "## [$version] - $date", "") + $body
}
else {
    $section = @("## [Unreleased]", "") + $body
}

$before = if ($start -gt 0) { $lines[0..($start - 1)] } else { @() }
$after = if ($end -lt $lines.Count) { $lines[$end..($lines.Count - 1)] } else { @() }
[IO.File]::WriteAllText($changelogPath, (@($before) + $section + @($after)) -join "`n")

if ($isRelease) {
    foreach ($folder in $sections.Keys) {
        foreach ($file in $files[$folder]) {
            Remove-Item -Path $file.FullName
        }
    }
}
$count = ($files.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum
Write-Information "Merged $count changelog entries into the $version section of $changelogPath" -InformationAction Continue
