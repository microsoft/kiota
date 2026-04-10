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

function Retry([Action]$action) {
    $attempts = 10
    $sleepInSeconds = 1
    do {
        try {
            $action.Invoke();
            break;
        }
        catch [Exception] {
            Write-Host $_.Exception.Message
        }            
        $attempts--
        if ($attempts -gt 0) { sleep $sleepInSeconds }
    } while ($attempts -gt 0)
}

$testPath = Join-Path -Path $PSScriptRoot -ChildPath $language
$mockServerPath = Join-Path -Path $PSScriptRoot -ChildPath "mockserver"

function Kill-MockServer {
    Push-Location $mockServerPath
    mvn --batch-mode mockserver:stopForked
    Pop-Location
}

$mockSeverITFolder = "inexistent-it-folder"
$configPath = Join-Path -Path $PSScriptRoot -ChildPath "config.json"
$jsonValue = Get-Content -Path $configPath -Raw | ConvertFrom-Json
$descriptionValue = $jsonValue.psobject.properties.Where({ $_.name -eq $descriptionUrl }).value
if ($null -ne $descriptionValue) {
    if ($descriptionValue.PSObject.Properties.Name -contains "MockServerITFolder") {
        $mockSeverITFolder = $descriptionValue.MockServerITFolder
    }
}

$mockServerTest = $false
if ($language -eq "ruby") {
    # Ruby places the integration tests in a subdir "spec"
    $itTestPath = Join-Path -Path $testPath -ChildPath "spec" -AdditionalChildPath $mockSeverITFolder
    if (Test-Path -Path $itTestPath) {
        $mockServerTest = $true
    }
}
else {
    $itTestPath = Join-Path -Path $testPath -ChildPath $mockSeverITFolder
    if (Test-Path -Path $itTestPath) {
        $mockServerTest = $true
    }
}

# Start MockServer if needed
if ($mockServerTest) {
    # Kill any leftover MockServer
    Kill-MockServer
    Push-Location $mockServerPath
    mvn  --batch-mode mockserver:runForked
    Pop-Location

    # Provision Mock server with the right spec
    #api description might be a JSON or a yaml file.
    if ($descriptionUrl.ToLower().EndsWith(".json")) {
        $openapiUrl = (Join-Path -Path $PSScriptRoot -ChildPath "openapi.json") -replace '\\', '/'
    }
    else {
        $openapiUrl = (Join-Path -Path $PSScriptRoot -ChildPath "openapi.yaml") -replace '\\', '/'
    }
    # provision MockServer to mock the specific openapi description https://www.mock-server.com/mock_server/using_openapi.html#button_open_api_filepath
    Retry({ Invoke-WebRequest -Method PUT -Body "{ `"specUrlOrPayload`": `"$openapiUrl`" }" -Uri http://localhost:1080/mockserver/openapi -ContentType application/json })
}

Push-Location $testPath
if ($language -eq "csharp") {
    if ($mockServerTest) {
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "client"
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "client"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        #Copy additional files if they do not exist already - this avoids duplication of files that do not really differ from the template.
        [string[]]$cleanupAfterTest = @()
        if (-not (Test-Path "$itTestPath\*.csproj") ) {
            #Copy default project file if none is present (also check for different name)
            Copy-Item -Path "$testPath\basic\basic.csproj" -Destination "$itTestPath"
            $cleanupAfterTest += "$itTestPath\basic.csproj"
        }
        if (-not (Test-Path "$itTestPath\Usings.cs") ) {
            Copy-Item -Path "$testPath\basic\Usings.cs" -Destination "$itTestPath"
            $cleanupAfterTest += "$itTestPath\Usings.cs"
        }

        try {
            Invoke-Call -ScriptBlock {
                dotnet test
            } -ErrorAction Stop
        }
        finally  {
            #Revert default items after the test (so that future runs will always pick the latest version from "basic")
            foreach($cleanupFile in $cleanupAfterTest) {
                Remove-Item $cleanupFile
            }

            Pop-Location
        }
    }
    else {
        Invoke-Call -ScriptBlock {
            dotnet build
        } -ErrorAction Stop
    }
}
elseif ($language -eq "java") {
    if ($mockServerTest) {
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "src" -AdditionalChildPath "*"
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "src" -AdditionalChildPath "main", "java"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        #Copy additional files if they do not exist already - this avoids duplication of files that do not really differ from the template.
        [string[]]$cleanupAfterTest = @()
        if (-not (Test-Path "$itTestPath\pom.xml") ) {
            #Copy default project file if none is present (also check for different name)
            Copy-Item -Path "$testPath\basic\pom.xml" -Destination "$itTestPath"
            $cleanupAfterTest += "$itTestPath\pom.xml"
        }

        try {
            Invoke-Call -ScriptBlock {
                mvn clean test --batch-mode
            } -ErrorAction Stop
        }
        finally {
            #Revert default items after the test (so that future runs will always pick the latest version from "basic")
            foreach($cleanupFile in $cleanupAfterTest) {
                Remove-Item $cleanupFile
            }

            Pop-Location
        }
    }
    else {
        Invoke-Call -ScriptBlock {
            mvn clean compile --batch-mode
        } -ErrorAction Stop
    }
}
elseif ($language -eq "go") {
    if ($mockServerTest) {
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "client"
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "client"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        #Copy additional files if they do not exist already - this avoids duplication of files that do not really differ from the template.
        [string[]]$cleanupAfterTest = @()
        if (-not (Test-Path "$itTestPath\go.mod") ) {
            #Copy default project file if none is present (also check for different name)
            Copy-Item -Path "$testPath\basic\go.mod" -Destination "$itTestPath"
            $cleanupAfterTest += "$itTestPath\go.mod"
        }
        if (-not (Test-Path "$itTestPath\go.sum") ) {
            Copy-Item -Path "$testPath\basic\go.sum" -Destination "$itTestPath"
            $cleanupAfterTest += "$itTestPath\go.sum"
        }

        try {
            Invoke-Call -ScriptBlock {
                go test
            } -ErrorAction Stop
        }
        finally  {
            #Revert default items after the test (so that future runs will always pick the latest version from "basic")
            foreach($cleanupFile in $cleanupAfterTest) {
                Remove-Item $cleanupFile
            }

            Pop-Location
        }
    }
    else {
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
    # Mock server tests are places in a subdir in "spec". So execute them.
    if ($mockServerTest) {
        Invoke-call -ScriptBlock {
            git init
            bundle install
            bundle exec rake SPEC=spec/$mockSeverITFolder/*.rb
        }
    }
    else {
        # Just execute the default test
        Invoke-call -ScriptBlock {
            git init
            bundle install
            bundle exec rake
        }
    }
}
elseif ($language -eq "php") {
    if ($mockServerTest) {
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "src/client"
        #Destination Path: use upper case "Client" here, as the test imports the namespace "Integration\Test\Client\...",
        #and as composer.json defines the autoload/psr-4 path "Integration\Test\", PHP would search the sub-namespaces in subdirs with the same casing.
        #That's relevant for the Github CI testsuite running on Linux.
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "src/Client"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        #Copy additional files if they do not exist already - this avoids duplication of files that do not really differ from the template.
        [string[]]$cleanupAfterTest = @()
        if (-not (Test-Path "$itTestPath\composer.json") ) {
            #Copy default project file if none is present (also check for different name)
            Copy-Item -Path "$testPath\basic\composer.json" -Destination "$itTestPath"
            $cleanupAfterTest += "$itTestPath\composer.json"
        }
        if (-not (Test-Path "$itTestPath\phpstan.neon") ) {
            Copy-Item -Path "$testPath\basic\phpstan.neon" -Destination "$itTestPath"
            $cleanupAfterTest += "$itTestPath\phpstan.neon"
        }

        try {
            Invoke-Call -ScriptBlock {
                composer install
                ./vendor/bin/phpstan
                #Execute all tests in subdir "tests"
                ./vendor/bin/phpunit tests
            } -ErrorAction Stop
        }
        finally  {
            #Revert default items after the test (so that future runs will always pick the latest version from "basic")
            foreach($cleanupFile in $cleanupAfterTest) {
                Remove-Item $cleanupFile
            }

            Pop-Location
        }
    }
    else {
        Invoke-Call -ScriptBlock {
            composer install
            ./vendor/bin/phpstan
        } -ErrorAction Stop
    }
}
elseif ($language -eq "python") {
    Invoke-Call -ScriptBlock {
        python -m pip install --upgrade pip
        pip install -r requirements-dev.txt
        pylint integration_test --disable=W --rcfile=.pylintrc
        mypy integration_test
    } -ErrorAction Stop

    if ($mockServerTest) {
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "integration_test" -AdditionalChildPath "client"
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "client"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        Invoke-Call -ScriptBlock {
            pytest
        } -ErrorAction Stop

        Pop-Location
    }
}
elseif ($language -eq "dart") {
    Invoke-Call -ScriptBlock {
        dart pub get
        dart analyze lib/
    } -ErrorAction Stop

    if ($mockServerTest) {
        Push-Location $itTestPath

        $itTestPathSources = Join-Path -Path $testPath -ChildPath "lib"
        $itTestPathDest = Join-Path -Path $itTestPath -ChildPath "lib"
        if (Test-Path $itTestPathDest) {
            Remove-Item $itTestPathDest -Force -Recurse
        }
        Copy-Item -Path $itTestPathSources -Destination $itTestPathDest -Recurse

        Invoke-Call -ScriptBlock {
            dart test
        } -ErrorAction Stop

        Pop-Location
    }
}
Pop-Location

if (!([string]::IsNullOrEmpty($mockSeverITFolder))) {
    Kill-MockServer
}
