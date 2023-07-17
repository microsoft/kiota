param (
    [string]
    [Parameter(Mandatory = $true)]
    $currentBranch,
    [string]
    [Parameter(Mandatory = $true)]
    $previewBranch,
    [switch]
    [bool]
    $excludeHeadingDash,
    [switch]
    [bool]
    $isGHA
)
if ($currentBranch -eq $previewBranch) {
    $buildSequenceNumber = [int]($Env:BUILD_BUILDNUMBER -split "\." | Select-Object -Last 1)
    $versionSuffix = "-preview." + (Get-Date).ToString("yyyyMMdd") + $buildSequenceNumber.ToString("0000")
    if ($excludeHeadingDash) {
        $versionSuffix = $versionSuffix.Substring(1)
    }
    if ($isGHA -eq $true) {
        Write-Output "versionSuffix=$versionSuffix" >> $Env:GITHUB_OUTPUT
        Write-Output "isPrerelease=true" >> $Env:GITHUB_OUTPUT
    }
    else {
        Write-Host "##vso[task.setvariable variable=versionSuffix]$versionSuffix"
        Write-Host "##vso[task.setvariable variable=isPrerelease]true"
    }
    Write-Output "Version suffix set to $versionSuffix"
}
else {
    if ($isGHA -eq $true) {
        Write-Output "versionSuffix=" >> $Env:GITHUB_OUTPUT
        Write-Output "isPrerelease=false" >> $Env:GITHUB_OUTPUT
    }
    else {
        Write-Host "##vso[task.setvariable variable=versionSuffix]"
        Write-Host "##vso[task.setvariable variable=isPrerelease]false"
    }
    Write-Output "Version suffix set to empty"
}