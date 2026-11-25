#!/usr/bin/env pwsh
#Requires -Version 7.4

<#
.SYNOPSIS
    Health check for the Kiota dev container: every tool at the version CI uses, and optionally a build and
    test run of the repository inside the container.

.DESCRIPTION
    -Tier Tools  Checks every tool against the versions this repository pins for CI (global.json,
                 it/go/go.mod, it/dart/pubspec.yaml, .github/workflows/*.yml). Takes about a second.
    -Tier Build  Tools, then builds kiota.slnx and the VS Code extension.
    -Tier Full   Build, then runs the .NET tests, the npm package and VS Code extension tests, and one
                 integration test per language against MockServer.

    Run it yourself inside the container; .github/workflows/devcontainer.yml runs -Tier Full on amd64 and
    arm64. Full runs the integration tests in a temporary worktree (HEAD with your uncommitted changes to tracked
    files), so it/ is never changed, and stops before them if it/ or the description has local changes. Two Full
    runs at once collide on MockServer's port and fail.

.EXAMPLE
    pwsh .devcontainer/test-devcontainer.ps1 -Tier Full -Language python

.EXAMPLE
    ./.devcontainer/test-devcontainer.ps1 -Tier Full -Language python, java  # several: from pwsh, which parses the list
#>
[CmdletBinding()]
param(
    [ValidateSet('Tools', 'Build', 'Full')]
    [string] $Tier = 'Tools',

    [ValidateSet('csharp', 'go', 'java', 'python', 'ruby', 'php', 'dart', 'typescript')]
    [string[]] $Language = @('csharp', 'go', 'java', 'python', 'ruby', 'php', 'dart', 'typescript'),

    # Has MockServer tests for most languages and no suppressions in it/config.json.
    [string] $Description = './tests/Kiota.Builder.IntegrationTests/InheritingErrors.yaml'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot DevContainer.psm1) -Force

# Exit in finally: a run stopped with Ctrl+C would otherwise end with exit code 0.
$code = 1
try {
    $code = Invoke-Check "tier $Tier" {
        Test-Toolchain
        if ($Tier -in 'Build', 'Full') { Invoke-Build }
        if ($Tier -eq 'Full') {
            Invoke-DotnetTest
            Invoke-ExtensionTest
            Invoke-IntegrationTest -Description $Description -Language $Language
        }
    }
}
finally { exit $code }
