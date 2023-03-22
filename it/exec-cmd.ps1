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
    Invoke-Expression "dotnet build"
}
elseif ($language -eq "java") {
    Invoke-Expression "${HOME}/.jbang/bin/jbang test.java"
}
elseif ($language -eq "go") {
    Invoke-Expression "go install && go build"
}
elseif ($language -eq "typescript") {
    Invoke-Expression "npm install && npm run build"
}
elseif ($language -eq "ruby") {
    $clientRb = Join-Path -Path $testPath -ChildPath "client.rb"
    $dest = Join-Path -Path $testPath -ChildPath "lib" -AdditionalChildPath "integration_test", "client"
    Copy-Item -Path $clientRb -Destination $dest
    Invoke-Expression "git init && bundle install && bundle exec rake"
}
elseif ($language -eq "php") {
    Invoke-Expression "composer install && ./vendor/bin/phpstan"
}
elseif ($language -eq "python") {
    Invoke-Expression "python -m pip install --upgrade pip && pip install pipenv && pipenv install --dev --skip-lock && pipenv run pylint integration_test --disable=W --rcfile=.pylintrc && pipenv run mypy integration_test"
}
Pop-Location
