#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl,
    [Parameter(Mandatory = $true)][string]$language
)

if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}

if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
    exit 1
}

# Ensure we exit with error in case of failure
# https://stackoverflow.com/a/52784160
function Invoke-Call {
    param (
        [scriptblock]$ScriptBlock,
        [string]$ErrorAction = $ErrorActionPreference
    )
    & @ScriptBlock
    if (($lastexitcode -ne 0) -and $ErrorAction -eq "Stop") {
        exit $lastexitcode
    }
}

function Retry([Action]$action)
{
    $attempts=10
    $sleepInSeconds=1
    do
    {
        try
        {
            $action.Invoke();
            break;
        }
        catch [Exception]
        {
            Write-Host $_.Exception.Message
        }            
        $attempts--
        if ($attempts -gt 0) { sleep $sleepInSeconds }
    } while ($attempts -gt 0)
}

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$testPath = Join-Path -Path $scriptPath -ChildPath $language
$mockServerPath = Join-Path -Path $scriptPath -ChildPath "mockserver"

function Kill-MockServer {
    Push-Location $mockServerPath
        mvn --batch-mode mockserver:stopForked
    Pop-Location
}

$mockSeverITFolder = $null
$configPath = Join-Path -Path $scriptPath -ChildPath "config.json"
$jsonValue = Get-Content -Path $configPath -Raw | ConvertFrom-Json
$descriptionValue = $jsonValue.psobject.properties.Where({ $_.name -eq $descriptionUrl }).value
if ($null -ne $descriptionValue) {
    if ($descriptionValue.PSObject.Properties.Name -contains "MockServerITFolder") {
        $mockSeverITFolder = $descriptionValue.MockServerITFolder
    }
}

# Kill any leftover MockServer
Kill-MockServer
# Start MockServer if needed
if (!([string]::IsNullOrEmpty($mockSeverITFolder))) {
    Push-Location $mockServerPath
        mvn  --batch-mode mockserver:runForked
    Pop-Location

    # Provision Mock server with the right spec
    $openapiUrl = $descriptionUrl
    if ($openapiUrl.StartsWith("./")) {
        $rootPath = Split-Path -parent $scriptPath
        $openapiUrl = $openapiUrl.replace("./", "file:$rootPath/", 1)
    }
    
    # provision MockServer to mock the specific openapi description https://www.mock-server.com/mock_server/using_openapi.html#button_open_api_filepath
    Retry({Invoke-WebRequest -Method PUT -Body "{ `"specUrlOrPayload`": `"$openapiUrl`" }" -Uri http://localhost:1080/mockserver/openapi -ContentType application/json})
}

Push-Location $testPath
if ($language -eq "csharp") {
    if (!([string]::IsNullOrEmpty($mockSeverITFolder))) {
        $itTestPath = Join-Path -Path $testPath -ChildPath $mockSeverITFolder
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "client"
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "client"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        Invoke-Call -ScriptBlock {
            dotnet test
        } -ErrorAction Stop

        Pop-Location
    } else {
        Invoke-Call -ScriptBlock {
            dotnet build
        } -ErrorAction Stop
    }
}
elseif ($language -eq "java") {
    if (!([string]::IsNullOrEmpty($mockSeverITFolder))) {
        $itTestPath = Join-Path -Path $testPath -ChildPath $mockSeverITFolder
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "src" -AdditionalChildPath "*"
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "src" -AdditionalChildPath "main", "java"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        Invoke-Call -ScriptBlock {
            mvn clean test --batch-mode
        } -ErrorAction Stop

        Pop-Location
    } else {
        Invoke-Call -ScriptBlock {
            mvn clean compile --batch-mode
        } -ErrorAction Stop
    }
}
elseif ($language -eq "go") {
    if (!([string]::IsNullOrEmpty($mockSeverITFolder))) {
        $itTestPath = Join-Path -Path $testPath -ChildPath $mockSeverITFolder
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "client"
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "client"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        Invoke-Call -ScriptBlock {
            go test
        } -ErrorAction Stop

        Pop-Location
    } else {
        Invoke-Call -ScriptBlock {
            go install
            go build
        } -ErrorAction Stop
    }
}
elseif ($language -eq "typescript") {
    Invoke-Call -ScriptBlock {
        npm install
        npm run build
    } -ErrorAction Stop
}
elseif ($language -eq "ruby") {
    $clientRb = Join-Path -Path $testPath -ChildPath "client.rb"
    $dest = Join-Path -Path $testPath -ChildPath "lib" -AdditionalChildPath "integration_test", "client"
    Copy-Item -Path $clientRb -Destination $dest
    Invoke-call -ScriptBlock {
        git init
        bundle install
        bundle exec rake
    }
}
elseif ($language -eq "php") {
    Invoke-Call -ScriptBlock {
        composer install
        ./vendor/bin/phpstan
    } -ErrorAction Stop
}
elseif ($language -eq "python") {
    Invoke-Call -ScriptBlock {
        python -m pip install --upgrade pip
        pip install pipenv
        pipenv install --dev --skip-lock
        pipenv run pylint integration_test --disable=W --rcfile=.pylintrc
        pipenv run mypy integration_test
    } -ErrorAction Stop
}
Pop-Location
Kill-MockServer
