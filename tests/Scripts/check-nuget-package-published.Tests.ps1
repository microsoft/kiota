# Run with: pwsh -NoProfile -File tests\Scripts\check-nuget-package-published.Tests.ps1
$ErrorActionPreference = 'Stop'
$checkScript = Join-Path $PSScriptRoot '..\..\scripts\check-nuget-package-published.ps1'
$packageDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "kiota-nuget-check-$([guid]::NewGuid())"
$serviceIndexUrl = 'https://pkgs.dev.azure.com/test/project/_packaging/feed/nuget/v3/index.json'
$packageBaseUrl = 'https://test.pkgs.visualstudio.com/project/_packaging/feed/nuget/v3/flat2/'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Invoke-RestMethod {
    param([string]$Uri, [hashtable]$Headers, [int]$MaximumRedirection)

    Assert-True ($Headers.Authorization -eq 'Bearer test-token') 'Missing feed authentication.'
    Assert-True ($MaximumRedirection -eq 0) 'Redirects must not bypass the private-feed check.'
    $requests.Add($Uri)
    if (($Uri -eq $serviceIndexUrl -and $testCase.IndexStatus) -or
        ($Uri -ne $serviceIndexUrl -and $testCase.Status)) {
        $status = if ($Uri -eq $serviceIndexUrl) { $testCase.IndexStatus } else { $testCase.Status }
        $response = [System.Net.Http.HttpResponseMessage]::new([System.Net.HttpStatusCode]$status)
        throw [Microsoft.PowerShell.Commands.HttpResponseException]::new("HTTP $status", $response)
    }
    if ($Uri -eq $serviceIndexUrl) {
        $baseUrl = if ($testCase.PublicResource) { 'https://api.nuget.org/v3-flatcontainer/' } else { $packageBaseUrl }
        $resources = if ($testCase.MissingResource) { @() } else {
            @(@{ '@type' = 'PackageBaseAddress/3.0.0'; '@id' = $baseUrl })
        }
        return @{ resources = $resources }
    }
    $expectedUrl = "$packageBaseUrl$($testCase.PackageId.ToLowerInvariant())/index.json"
    Assert-True ($Uri -eq $expectedUrl) "Unexpected package lookup URL: $Uri"
    if ($testCase.MalformedResponse) { return @{} }
    return @{ versions = $testCase.Versions }
}

$cases = @(
    @{ Name = 'Published CLI'; PackageId = 'Microsoft.OpenApi.Kiota'; Versions = @('1.36.0'); Expected = 'true' }
    @{ Name = 'Published builder'; PackageId = 'Microsoft.OpenApi.Kiota.Builder'; Versions = @('1.36.0'); Expected = 'true' }
    @{ Name = 'Unpublished version'; PackageId = 'Microsoft.OpenApi.Kiota'; Versions = @('1.35.0'); Expected = 'false' }
    @{ Name = 'Published prerelease'; PackageId = 'Microsoft.OpenApi.Kiota'; Version = '1.37.0-preview.1'; Versions = @('1.37.0-preview.1'); Expected = 'true' }
    @{ Name = 'Missing package'; PackageId = 'Microsoft.OpenApi.Kiota'; Status = 404; Expected = 'false' }
    @{ Name = 'Unauthorized'; PackageId = 'Microsoft.OpenApi.Kiota'; Status = 401; Error = 'HTTP 401' }
    @{ Name = 'Forbidden'; PackageId = 'Microsoft.OpenApi.Kiota'; Status = 403; Error = 'HTTP 403' }
    @{ Name = 'Feed unavailable'; PackageId = 'Microsoft.OpenApi.Kiota'; Status = 500; Error = 'HTTP 500' }
    @{ Name = 'Missing service index'; PackageId = 'Microsoft.OpenApi.Kiota'; IndexStatus = 404; Error = 'HTTP 404' }
    @{ Name = 'Missing artifact'; PackageId = 'Missing.Package'; Error = 'No Missing.Package nupkg' }
    @{ Name = 'Missing content resource'; PackageId = 'Microsoft.OpenApi.Kiota'; MissingResource = $true; Error = 'No PackageBaseAddress resource' }
    @{ Name = 'Public content resource'; PackageId = 'Microsoft.OpenApi.Kiota'; PublicResource = $true; Error = 'HTTPS Azure Artifacts feed' }
    @{ Name = 'Public service index'; PackageId = 'Microsoft.OpenApi.Kiota'; PublicIndex = $true; Error = 'HTTPS Azure Artifacts feed' }
    @{ Name = 'Missing token'; PackageId = 'Microsoft.OpenApi.Kiota'; MissingToken = $true; Error = 'FEED_ACCESS_TOKEN is required' }
    @{ Name = 'Malformed version response'; PackageId = 'Microsoft.OpenApi.Kiota'; MalformedResponse = $true; Error = 'No versions returned' }
)

try {
    $null = New-Item -Path $packageDirectory -ItemType Directory
    # The builder artifact must not be mistaken for the CLI artifact.
    $null = New-Item -Path (Join-Path $packageDirectory 'Microsoft.OpenApi.Kiota.Builder.1.36.0.nupkg') -ItemType File
    foreach ($testCase in $cases) {
        $requests = [System.Collections.Generic.List[string]]::new()
        $version = if ($testCase.Version) { $testCase.Version } else { '1.36.0' }
        $cliArtifact = Join-Path $packageDirectory "Microsoft.OpenApi.Kiota.$version.nupkg"
        $null = New-Item -Path $cliArtifact -ItemType File
        $arguments = @{
            PackageDirectory = $packageDirectory
            PackageId = $testCase.PackageId
            NuGetServiceIndexUrl = if ($testCase.PublicIndex) { 'https://api.nuget.org/v3/index.json' } else { $serviceIndexUrl }
            FeedAccessToken = if ($testCase.MissingToken) { '' } else { 'test-token' }
        }
        $output = @()
        $failure = $null
        try {
            $output = @(& $checkScript @arguments 6>&1)
        }
        catch {
            $failure = $_
        }
        finally {
            Remove-Item -LiteralPath $cliArtifact
        }

        if ($testCase.Error) {
            Assert-True ($null -ne $failure) "$($testCase.Name): expected lookup failure."
            Assert-True ($failure.Exception.Message.Contains($testCase.Error)) "$($testCase.Name): unexpected error: $failure"
            Assert-True (-not ($output -match 'task.setvariable')) "$($testCase.Name): failed lookup must not permit publishing."
        }
        else {
            Assert-True ($null -eq $failure) "$($testCase.Name): unexpected failure: $failure`n$($failure.ScriptStackTrace)"
            $expected = "##vso[task.setvariable variable=nugetAlreadyPublished]$($testCase.Expected)"
            Assert-True ($output.ForEach({ $_.ToString() }) -contains $expected) "$($testCase.Name): incorrect pipeline variable."
            Assert-True ($requests.Count -eq 2) "$($testCase.Name): expected service index and package lookups."
        }
        if ($testCase.PublicIndex -or $testCase.MissingToken -or $testCase.PackageId -eq 'Missing.Package') {
            Assert-True ($requests.Count -eq 0) "$($testCase.Name): should fail before making a request."
        }
        if ($testCase.PublicResource) {
            Assert-True ($requests.Count -eq 1) 'Public endpoint must not be contacted.'
        }
        Write-Host "PASS: $($testCase.Name)"
    }
    Write-Host "All $($cases.Count) NuGet publication checks passed."
}
finally {
    Remove-Item -LiteralPath $packageDirectory -Recurse
}
