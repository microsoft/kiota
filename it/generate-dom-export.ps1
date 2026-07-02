#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Generates the kiota public-API DOM export (kiota-dom-export.txt) for a single
    generator executable, OpenAPI description and language.

.DESCRIPTION
    Enables the public API export feature (Generation:ExportPublicApi) through the
    KIOTA_ prefixed environment variable and runs `kiota generate`. The resulting
    "kiota-dom-export.txt" surface file is written to the output directory.

    The same flags must be used for the baseline (published) and current (changeset)
    generators so that any difference in the export reflects a real change in the
    generated public API surface rather than a difference in invocation.

.PARAMETER kiotaExec
    Path to the kiota executable to run.

.PARAMETER openapiPath
    Path to the local OpenAPI description file.

.PARAMETER language
    Target generation language (e.g. csharp, java).

.PARAMETER outputPath
    Directory where the generated code and kiota-dom-export.txt are written.

.PARAMETER additionalArguments
    Optional extra arguments forwarded verbatim to `kiota generate` (e.g. exclude patterns).

.OUTPUTS
    The full path to the generated kiota-dom-export.txt file.
#>

param(
    [Parameter(Mandatory = $true)][string]$kiotaExec,
    [Parameter(Mandatory = $true)][string]$openapiPath,
    [Parameter(Mandatory = $true)][string]$language,
    [Parameter(Mandatory = $true)][string]$outputPath,
    [Parameter(Mandatory = $false)][string]$additionalArguments = ""
)

if ([string]::IsNullOrEmpty($kiotaExec)) {
    Write-Error "kiota executable path is empty"
    exit 1
}
if (-not (Test-Path $kiotaExec)) {
    Write-Error "kiota executable not found at '$kiotaExec'"
    exit 1
}
if ([string]::IsNullOrEmpty($openapiPath) -or -not (Test-Path $openapiPath)) {
    Write-Error "OpenAPI description not found at '$openapiPath'"
    exit 1
}
if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}
if ([string]::IsNullOrEmpty($outputPath)) {
    Write-Error "Output path is empty"
    exit 1
}

# Disable update checks / tutorials so they do not pollute the run.
$Env:KIOTA_TUTORIAL_ENABLED = "false"
$Env:KIOTA_OFFLINE_ENABLED = "true"
# Enable the public API (DOM) export. Config key is Generation:ExportPublicApi and the
# host binds environment variables with the KIOTA_ prefix, using __ as the section separator.
$Env:KIOTA_Generation__ExportPublicApi = "true"

$generateArguments = "generate --exclude-backward-compatible --clean-output --language ${language} --openapi `"${openapiPath}`" --output `"${outputPath}`"${additionalArguments}"

Write-Output "Generating DOM export: $kiotaExec $generateArguments"
$generationProcess = Start-Process "$kiotaExec" -ArgumentList $generateArguments -Wait -NoNewWindow -PassThru

if ($generationProcess.ExitCode -ne 0) {
    Write-Error "Failed to generate the code/DOM export for '${language}' (exit code $($generationProcess.ExitCode))"
    exit 1
}

$domExportPath = Join-Path -Path $outputPath -ChildPath "kiota-dom-export.txt"
if (-not (Test-Path $domExportPath)) {
    Write-Error "DOM export file was not produced at '$domExportPath'. Ensure the generator supports the public API export feature."
    exit 1
}

Write-Output "DOM export written to $domExportPath"
return $domExportPath
