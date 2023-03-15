#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$descriptionUrl
)

if ([string]::IsNullOrEmpty($descriptionUrl)) {
    Write-Error "Description URL is empty"
    exit 1
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

$mockServerUrl="https://repo1.maven.org/maven2/org/mock-server/mockserver-netty/5.14.0/mockserver-netty-5.14.0-shaded.jar"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootPath = Split-Path -parent $scriptPath
$mockServerPath = Join-Path -Path $scriptPath -ChildPath "mockserver.jar"

if(-not(Test-path $mockServerPath -PathType leaf)) {
  Invoke-WebRequest $mockServerUrl -OutFile $mockServerPath
}

# start the mock server in background
Start-Process java -ArgumentList "-jar", "$mockServerPath", "-serverPort", "1080", "-logLevel", "DEBUG"

if ($descriptionUrl.StartsWith("./")) {
  $descriptionUrl = $descriptionUrl.replace("./", "file:$rootPath/", 1)
}
Write-Error $descriptionUrl

Retry({Invoke-WebRequest -Method PUT -Body "{ `"specUrlOrPayload`": `"$descriptionUrl`"}" -Uri http://localhost:1080/mockserver/openapi -ContentType application/json})
