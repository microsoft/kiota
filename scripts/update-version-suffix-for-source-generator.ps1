param (
    [string]
    $versionSuffix
)

$versionSuffix = $versionSuffix.TrimStart("-")
if ($versionSuffix -eq "") {
    return
}

# Get current script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$appCsProjPath = Join-Path -Path $scriptPath -ChildPath "../src/kiota/kiota.csproj"
$builderCsProjPath = Join-Path -Path $scriptPath -ChildPath "../src/Kiota.Builder/Kiota.Builder.csproj"

#load xml from csproj
$appCsProj = [xml](Get-Content $appCsProjPath)
$builderCsProj = [xml](Get-Content $builderCsProjPath)

#update version suffix
$appCsProj.Project.PropertyGroup[0].VersionSuffix = $versionSuffix
$builderCsProj.Project.PropertyGroup[0].VersionSuffix = $versionSuffix

#save xml to csproj
$appCsProj.Save($appCsProjPath)
$builderCsProj.Save($builderCsProjPath)
