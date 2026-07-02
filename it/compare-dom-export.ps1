#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Compares the kiota public-API DOM export produced by the currently published
    `Microsoft.OpenApi.Kiota` NuGet tool (baseline) against the export produced by a
    locally built kiota (current changeset) and emits a unified diff (patch).

.DESCRIPTION
    This is the orchestrator behind the "surface area" regression check. It detects
    binary/source breaking changes that a change to kiota's generation logic could
    introduce into downstream SDKs (e.g. the Microsoft Graph SDKs).

    Steps:
      1. Resolve / download the OpenAPI description to a single local file (used by both
         generators for determinism).
      2. Install the published Microsoft.OpenApi.Kiota tool and generate the BASELINE export.
      3. Use the locally built kiota to generate the CURRENT export.
      4. Produce a unified diff (patch) between baseline and current exports.

    The patch is intended to be fed to the
    `microsoftgraph/kiota-dom-export-diff-tool/tool` action (with fail-on-removal: true)
    which decides pass/fail. For local runs this script also prints a summary of added /
    removed surface lines.

    NOTE (accumulation caveat): the baseline is the last published release, so the diff
    reflects EVERY change since that release, not only the current changeset.

.PARAMETER descriptionUrl
    OpenAPI description: an http(s) URL, a local path, or a kiota search key.

.PARAMETER language
    Target generation language (e.g. csharp, java).

.PARAMETER patchPath
    Path of the unified diff file to write. Defaults to ./<language>-dom-export.patch.

.PARAMETER baselineVersion
    Optional specific version of Microsoft.OpenApi.Kiota to use as baseline.
    Defaults to the latest stable release.

.PARAMETER kiotaExec
    Path to the locally built kiota executable (current changeset). Defaults to ./publish/kiota.

.PARAMETER additionalArguments
    Optional extra arguments forwarded verbatim to both `kiota generate` invocations.

.PARAMETER workingDirectory
    Directory used for intermediate artifacts (spec, tool install, exports). Defaults to a temp dir.
#>

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl,
    [Parameter(Mandatory = $true)][string]$language,
    [Parameter(Mandatory = $false)][string]$patchPath,
    [Parameter(Mandatory = $false)][string]$baselineVersion,
    [Parameter(Mandatory = $false)][string]$kiotaExec,
    [Parameter(Mandatory = $false)][string]$additionalArguments = "",
    [Parameter(Mandatory = $false)][string]$workingDirectory
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
    exit 1
}
if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}

$rootPath = Join-Path -Path $PSScriptRoot -ChildPath ".."
$executableName = "kiota"
if ($IsWindows) {
    $executableName = "kiota.exe"
}

# Resolve the locally built (current changeset) kiota executable.
if ([string]::IsNullOrEmpty($kiotaExec)) {
    $kiotaExec = Join-Path -Path $rootPath -ChildPath "publish" -AdditionalChildPath $executableName
}
if (-not (Test-Path $kiotaExec)) {
    Write-Error "Locally built kiota not found at '$kiotaExec'. Publish it first, e.g. `dotnet publish ./src/kiota/kiota.csproj -c Release -o ./publish -f net10.0`."
    exit 1
}

# Working directory for intermediate artifacts.
if ([string]::IsNullOrEmpty($workingDirectory)) {
    $workingDirectory = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ([System.Guid]::NewGuid().ToString())
}
New-Item -ItemType Directory -Path $workingDirectory -Force | Out-Null
Write-Output "Working directory: $workingDirectory"

if ([string]::IsNullOrEmpty($patchPath)) {
    $patchPath = Join-Path -Path (Get-Location) -ChildPath "$language-dom-export.patch"
}

$Env:KIOTA_TUTORIAL_ENABLED = "false"

# --- 1. Resolve the OpenAPI description to a single local file -----------------------------
if ($descriptionUrl.ToLower().EndsWith(".json")) {
    $targetOpenapiPath = Join-Path -Path $workingDirectory -ChildPath "openapi.json"
}
else {
    $targetOpenapiPath = Join-Path -Path $workingDirectory -ChildPath "openapi.yaml"
}

if ($descriptionUrl.StartsWith("./") -or $descriptionUrl.StartsWith(".\") -or
    ($descriptionUrl.Length -gt 1 -and ($descriptionUrl.Substring(1).StartsWith(":\") -or $descriptionUrl.Substring(1).StartsWith(":/"))) -or
    $descriptionUrl.StartsWith("\") -or $descriptionUrl.StartsWith("/")) {
    Copy-Item -Path $descriptionUrl -Destination $targetOpenapiPath -Force
}
elseif ($descriptionUrl.StartsWith("http")) {
    Invoke-WebRequest -Uri $descriptionUrl -OutFile $targetOpenapiPath
}
else {
    $downloadProcess = Start-Process "$kiotaExec" -ArgumentList "download ${descriptionUrl} --clean-output --output `"$targetOpenapiPath`"" -Wait -NoNewWindow -PassThru
    if ($downloadProcess.ExitCode -ne 0) {
        Write-Error "Failed to download the openapi description"
        exit 1
    }
}
Write-Output "Using OpenAPI description: $targetOpenapiPath"

# --- 2. Install the published baseline tool ------------------------------------------------
$toolPath = Join-Path -Path $workingDirectory -ChildPath "baseline-tool"
$installArgs = @("tool", "install", "Microsoft.OpenApi.Kiota", "--tool-path", $toolPath)
if (-not [string]::IsNullOrEmpty($baselineVersion)) {
    $installArgs += @("--version", $baselineVersion)
}
Write-Output "Installing baseline tool: dotnet $($installArgs -join ' ')"
& dotnet @installArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to install the baseline Microsoft.OpenApi.Kiota tool"
    exit 1
}
$baselineKiotaExec = Join-Path -Path $toolPath -ChildPath $executableName
if (-not (Test-Path $baselineKiotaExec)) {
    Write-Error "Baseline kiota executable not found at '$baselineKiotaExec'"
    exit 1
}

# Report the resolved baseline version for traceability.
$resolvedBaseline = & $baselineKiotaExec --version 2>$null | Select-Object -First 1
Write-Output "Baseline kiota version: $resolvedBaseline"

# --- 3. Generate baseline and current exports ----------------------------------------------
$generateScript = Join-Path -Path $PSScriptRoot -ChildPath "generate-dom-export.ps1"
$baselineOutput = Join-Path -Path $workingDirectory -ChildPath "baseline"
$currentOutput = Join-Path -Path $workingDirectory -ChildPath "current"

Write-Output "Generating baseline DOM export..."
& $generateScript -kiotaExec $baselineKiotaExec -openapiPath $targetOpenapiPath -language $language -outputPath $baselineOutput -additionalArguments $additionalArguments | Out-Null
$baselineExport = Join-Path -Path $baselineOutput -ChildPath "kiota-dom-export.txt"

Write-Output "Generating current DOM export..."
& $generateScript -kiotaExec $kiotaExec -openapiPath $targetOpenapiPath -language $language -outputPath $currentOutput -additionalArguments $additionalArguments | Out-Null
$currentExport = Join-Path -Path $currentOutput -ChildPath "kiota-dom-export.txt"

if (-not (Test-Path $baselineExport)) {
    Write-Error "Baseline export missing at '$baselineExport'"
    exit 1
}
if (-not (Test-Path $currentExport)) {
    Write-Error "Current export missing at '$currentExport'"
    exit 1
}

# --- 4. Produce a unified diff (patch) -----------------------------------------------------
# The patch is consumed by microsoftgraph/kiota-dom-export-diff-tool, which expects a
# `git format-patch` style patch: zero context (-U0) so every body line is a + or - line,
# and a trailing "-- " signature footer (DOM export lines contain "-->" for inheritance, so
# the tool's footer detection relies on that trailing signature). We therefore build the
# patch the same way that tool's own export action does: commit the baseline then the current
# export into a throwaway git repository and run `git format-patch -1 HEAD --minimal -U0 -w`.

# Ensure the destination directory exists and remove any stale patch.
$patchDir = Split-Path -Parent $patchPath
if (-not [string]::IsNullOrEmpty($patchDir) -and -not (Test-Path $patchDir)) {
    New-Item -ItemType Directory -Path $patchDir -Force | Out-Null
}
if (Test-Path $patchPath) {
    Remove-Item -Force $patchPath
}

$baselineContent = Get-Content -Raw -Path $baselineExport
$currentContent = Get-Content -Raw -Path $currentExport

if ($baselineContent -ceq $currentContent) {
    Write-Output "No differences detected in the DOM export for '$language'. The public API surface is unchanged."
    # Emit an empty patch file so downstream steps can branch on its size.
    New-Item -ItemType File -Path $patchPath -Force | Out-Null
    return $patchPath
}

$patchRepo = Join-Path -Path $workingDirectory -ChildPath "patch-repo"
New-Item -ItemType Directory -Path $patchRepo -Force | Out-Null
$repoExport = Join-Path -Path $patchRepo -ChildPath "kiota-dom-export.txt"

# Inline identity + disabled signing so this works on CI runners with no global git config.
$gitConfig = @("-c", "user.email=kiota@microsoft.com", "-c", "user.name=kiota", "-c", "commit.gpgsign=false", "-c", "init.defaultBranch=main")

& git @gitConfig init $patchRepo | Out-Null
Copy-Item -Path $baselineExport -Destination $repoExport -Force
& git @gitConfig -C $patchRepo add kiota-dom-export.txt | Out-Null
& git @gitConfig -C $patchRepo commit -m "baseline" --quiet | Out-Null
Copy-Item -Path $currentExport -Destination $repoExport -Force
& git @gitConfig -C $patchRepo add kiota-dom-export.txt | Out-Null
& git @gitConfig -C $patchRepo commit -m "current" --quiet | Out-Null

$patchOutDir = Join-Path -Path $workingDirectory -ChildPath "patch-out"
New-Item -ItemType Directory -Path $patchOutDir -Force | Out-Null
& git @gitConfig -C $patchRepo format-patch -1 HEAD --minimal -U0 -w -o $patchOutDir | Out-Null

$generatedPatch = Get-ChildItem -Path $patchOutDir -Filter "*.patch" | Select-Object -First 1
if ($null -eq $generatedPatch) {
    Write-Error "Failed to generate a patch file with git format-patch"
    exit 1
}
Copy-Item -Path $generatedPatch.FullName -Destination $patchPath -Force

$patchLines = Get-Content -Path $patchPath
$removed = ($patchLines | Where-Object { $_.StartsWith("-") -and -not $_.StartsWith("---") }).Count
$added = ($patchLines | Where-Object { $_.StartsWith("+") -and -not $_.StartsWith("+++") }).Count
Write-Output "DOM export differs for '$language': $removed removed line(s), $added added line(s)."
Write-Output "Removed lines indicate potential source/binary breaking changes."
Write-Output "Patch written to: $patchPath"

return $patchPath
