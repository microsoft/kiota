param([Parameter(Mandatory=$true)][string]$username, [Parameter(Mandatory=$true)][string]$apiToken, [Parameter(Mandatory=$true)][string]$nugetFileAbsolutePath)
if(Test-Path -Path $nugetFileAbsolutePath) {
    [xml]$nugetConfigFileContent = Get-Content -Path $nugetFileAbsolutePath;
    $userEntry = $nugetConfigFileContent.configuration.packageSourceCredentials.GitHub.add | ? {$_.key -eq "Username"} | select -First 1
    $userEntry.value = $username
    $tokenEntry = $nugetConfigFileContent.configuration.packageSourceCredentials.GitHub.add | ? {$_.key -eq "ClearTextPassword"} | select -First 1
    $tokenEntry.value = $apiToken
    $nugetConfigFileContent.Save($nugetFileAbsolutePath)
} else {
    Write-Error -Message "Nuget config file not found, please check the path"
}