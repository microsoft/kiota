param (
    [string]
    [Parameter(Mandatory = $true)]
    $currentBranch,
    [string]
    [Parameter(Mandatory = $true)]
    $previewBranch
)
if ($currentBranch -eq $previewBranch) {
    $buildSequenceNumber = [int]($Env:BUILD_BUILDNUMBER -split "\." | Select-Object -Last 1)
    $versionSuffix = "-preview." + (Get-Date).ToString("yyyyMMdd") + $buildSequenceNumber.ToString("0000")
    Write-Host "##vso[task.setvariable variable=versionSuffix]$versionSuffix"
    Write-Host "##vso[task.setvariable variable=isPrerelease]true"
    Write-Output "Version suffix set to $versionSuffix"
}
else {
    Write-Output "Version suffix set to empty"
    Write-Host "##vso[task.setvariable variable=versionSuffix]"
    Write-Host "##vso[task.setvariable variable=isPrerelease]false"
}