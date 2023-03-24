param (
    [string]
    [Parameter(Mandatory = $true)]
    $nugetFolderPath
)

$artifactName = Get-ChildItem -Path $nugetFolderPath -Filter Microsoft.OpenApi.Kiota.*.nupkg -recurse | Select-Object -First 1
$artifactVersion = $artifactName.Name -replace "Microsoft.OpenApi.Kiota.", "" -replace ".nupkg", ""
Write-Host "##vso[task.setvariable variable=artifactVersion; isSecret=false; isOutput=true]$artifactVersion"
Write-Output "$artifactVersion"