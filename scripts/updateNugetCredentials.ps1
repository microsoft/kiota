param([Parameter(Mandatory=$true)][string]$username, [Parameter(Mandatory=$true)][string]$apiToken, [Parameter(Mandatory=$true)][string]$nugetFileAbsolutePath)
$template = "<?xml version=`"1.0`" encoding=`"utf-8`"?><configuration><packageSources><add key=`"GitHub`" value=`"https://nuget.pkg.github.com/microsoft/index.json`" /></packageSources><packageSourceCredentials><GitHub><add key=`"Username`" value=`"`" /><add key=`"ClearTextPassword`" value=`"`" /></GitHub></packageSourceCredentials></configuration>"
[xml]$nugetConfigFileContent = [xml]$template
$userEntry = $nugetConfigFileContent.configuration.packageSourceCredentials.GitHub.add | Where-Object {$_.key -eq "Username"} | Select-Object -First 1
$userEntry.value = $username
$tokenEntry = $nugetConfigFileContent.configuration.packageSourceCredentials.GitHub.add | Where-Object {$_.key -eq "ClearTextPassword"} | Select-Object -First 1
$tokenEntry.value = $apiToken
$nugetConfigFileContent.Save($nugetFileAbsolutePath)