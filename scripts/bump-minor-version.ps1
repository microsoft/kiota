$indentContentScriptPath = Join-Path -Path $PSScriptRoot -ChildPath "indent-xml-content.ps1"
. $indentContentScriptPath

$mainCSProjpath = Join-Path -Path $PSScriptRoot -ChildPath "../src/kiota/kiota.csproj"
$mainCsprojContent = [xml](Get-Content -Path $mainCSProjpath)
$version = $mainCsprojContent.Project.PropertyGroup[0].VersionPrefix
$versionParts = $version.Split(".")
$versionParts[1] = [int]$versionParts[1] + 1
$version = $versionParts -join "."
$mainCsprojContent.Project.PropertyGroup[0].VersionPrefix = $version
Format-XMLIndent -Content $mainCsprojContent | Set-Content -Path $mainCSProjpath -Encoding utf8
$builderCSProjPath = Join-Path -Path $PSScriptRoot -ChildPath "../src/Kiota.Builder/Kiota.Builder.csproj"
$builderCsprojContent = [xml](Get-Content -Path $builderCSProjPath)
$builderCsprojContent.Project.PropertyGroup[0].VersionPrefix = $version
Format-XMLIndent -Content $builderCsprojContent | Set-Content -Path $builderCSProjPath -Encoding utf8