#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$language
)

if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$testPath = Join-Path -Path $scriptPath -ChildPath $language

Push-Location $testPath
if ($language -eq "csharp") {
    Remove-Item obj -Recurse -ErrorAction SilentlyContinue
}
elseif ($language -eq "typescript") {
    Remove-Item node_modules -Recurse -ErrorAction SilentlyContinue
    Remove-Item dist -Recurse -ErrorAction SilentlyContinue
}
elseif ($language -eq "ruby") {
    Remove-Item vendor -Recurse -ErrorAction SilentlyContinue
    Remove-Item .git -Recurse -ErrorAction SilentlyContinue
    Remove-Item Gemfile.lock -ErrorAction SilentlyContinue
}
elseif ($language -eq "php") {
    Remove-Item vendor -Recurse -ErrorAction SilentlyContinue
    Remove-Item composer.lock -ErrorAction SilentlyContinue
}
Pop-Location
