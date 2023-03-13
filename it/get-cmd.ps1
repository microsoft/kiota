#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$language
)

if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}

$command = "exit 1";
if ($language -eq "csharp") {
    $command = "(cd ./it/csharp && dotnet build)"
}
elseif ($language -eq "java") {
    $command = "(cd ./it/java && ${HOME}/.jbang/bin/jbang test.java)"
}
elseif ($language -eq "go") {
    $command = "(cd ./it/go && go install && go build)"
}
elseif ($language -eq "typescript") {
    $command = "(cd ./it/typescript && npm install && npm run build)"
}
elseif ($language -eq "ruby") {
    $command = "(cd ./it/ruby && git init && bundle exec rake)"
}
elseif ($language -eq "php") {
    $command = "(cd ./it/php && composer install && ./vendor/bin/phpstan)"
}
elseif ($language -eq "python") {
    $command = "(cd ./it/python && python -m pip install --upgrade pip && pip install pipenv && pipenv install --dev --skip-lock && pipenv run pylint integration_test --disable=W --rcfile=.pylintrc && pipenv run mypy integration_test)"
}

return $command
