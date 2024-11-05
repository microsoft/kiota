# This script enables the shims for packaging the project as a tool
$csprojPath = Join-Path $PSScriptRoot "../src/kiota/kiota.csproj"

$xml = [Xml] (Get-Content $csprojPath)

$shimIdentifiers = $xml.CreateElement("PackAsToolShimRuntimeIdentifiers")
$shimIdentifiers.InnerText = "win-x64;win-x86;osx-x64"

$rootPropertyGroup = $xml.Project.PropertyGroup[0]
$rootPropertyGroup.AppendChild($shimIdentifiers)

#save xml to csproj
$xml.Save($csprojPath)