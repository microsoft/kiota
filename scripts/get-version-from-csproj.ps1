param (
    [switch]
    [bool]
    $isGHA
)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$csprojPath = Join-Path $scriptPath "../src/kiota/kiota.csproj"
$xml = [Xml] (Get-Content $csprojPath)
$version = $xml.Project.PropertyGroup.VersionPrefix[0]
Write-Output "csproj version is $version"
if ($isGHA -eq $true) {
    Write-Output "version=$version" >> $Env:GITHUB_OUTPUT

}
else {
    Write-Host "##vso[task.setvariable variable=artifactVersion]$version"
}
