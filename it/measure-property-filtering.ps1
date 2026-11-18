#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet('flat', 'diamond', 'cycle', 'file')][string[]]$Scenarios = @('flat', 'diamond', 'cycle'),
    [ValidateRange(1, 100000)][int]$FlatSize = 2048,
    [ValidateRange(1, 256)][int]$GraphDepth = 24,
    [string]$DescriptionPath,
    [string[]]$IncludeProperty = @('simple-user#login'),
    [ValidateRange(1, 20)][int]$Iterations = 3,
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 120,
    [ValidateRange(64, 65536)][int]$MaxWorkingSetMiB = 1024,
    [string]$OutputPath = (Join-Path $PWD 'property-filter-resources.json')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'tests' -AdditionalChildPath 'Kiota.Builder.Tests', 'Kiota.Builder.Tests.csproj'
& dotnet build $project --configuration Release --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Building the resource test runner failed. Restore its dependencies before retrying.' }
$executable = Join-Path (Split-Path $project) 'bin' -AdditionalChildPath 'Release', 'net10.0', 'Kiota.Builder.Tests'
if ($IsWindows) { $executable += '.exe' }
$descriptionHash = $null
if ($Scenarios -contains 'file') {
    if ([string]::IsNullOrWhiteSpace($DescriptionPath)) { throw '-DescriptionPath is required for the file scenario.' }
    $DescriptionPath = (Resolve-Path -LiteralPath $DescriptionPath).Path
    $descriptionHash = (Get-FileHash -LiteralPath $DescriptionPath -Algorithm SHA256).Hash
}

$samples = [System.Collections.Generic.List[object]]::new()
foreach ($scenario in $Scenarios) {
    $size = if ($scenario -eq 'flat') { $FlatSize } else { $GraphDepth }
    $includes = switch ($scenario) {
        'file' { $IncludeProperty }
        'diamond' { @('Target#keep', 'Root#keep') }
        'cycle' { @('Target#keep', 'Node0#keep') }
        default { @('Target#keep') }
    }
    foreach ($mode in @('baseline', 'filtered')) {
        for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
            $start = [System.Diagnostics.ProcessStartInfo]::new($executable)
            $start.UseShellExecute = $false
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            foreach ($argument in @('-class', 'Kiota.Builder.Tests.OpenApiPropertyFilterResourceTests', '-noLogo', '-showLiveOutput')) {
                $start.ArgumentList.Add($argument)
            }
            $start.Environment['KIOTA_PROPERTY_FILTER_RESOURCE_SCENARIO'] = $scenario
            $start.Environment['KIOTA_PROPERTY_FILTER_RESOURCE_SIZE'] = [string]$size
            $start.Environment['KIOTA_PROPERTY_FILTER_RESOURCE_MODE'] = $mode
            $start.Environment['KIOTA_PROPERTY_FILTER_RESOURCE_INCLUDES'] = ConvertTo-Json -InputObject @($includes) -Compress
            if ($scenario -eq 'file') { $start.Environment['KIOTA_PROPERTY_FILTER_RESOURCE_DESCRIPTION'] = $DescriptionPath }
            $process = [System.Diagnostics.Process]::Start($start)
            try {
                $stdout = $process.StandardOutput.ReadToEndAsync()
                $stderr = $process.StandardError.ReadToEndAsync()
                $watch = [System.Diagnostics.Stopwatch]::StartNew()
                while (-not $process.WaitForExit(200)) {
                    $process.Refresh()
                    if ($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds -or $process.PeakWorkingSet64 -gt $MaxWorkingSetMiB * 1MB) {
                        $process.Kill($true)
                        $process.WaitForExit()
                        throw "$scenario/$mode exceeded the ${TimeoutSeconds}s or ${MaxWorkingSetMiB}MiB process budget."
                    }
                }
                $text = $stdout.GetAwaiter().GetResult()
                $errors = $stderr.GetAwaiter().GetResult()
                if ($process.ExitCode -ne 0) { throw "$scenario/$mode failed:`n$text`n$errors" }
                $match = [regex]::Match($text, 'PROPERTY_FILTER_METRICS (\{[^\r\n]+\})')
                if (-not $match.Success) { throw "No resource metrics were returned:`n$text`n$errors" }
                $sample = $match.Groups[1].Value | ConvertFrom-Json
                if ($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds -or $sample.ProcessPeakWorkingSetBytes -gt $MaxWorkingSetMiB * 1MB) {
                    throw "$scenario/$mode exceeded the ${TimeoutSeconds}s or ${MaxWorkingSetMiB}MiB process budget."
                }
                $sample | Add-Member -NotePropertyName Iteration -NotePropertyValue $iteration
                $samples.Add($sample)
                Write-Host ("{0}/{1}: {2:N1} ms, {3:N1} MiB allocated, {4:N1} MiB process peak" -f
                    $scenario, $mode, $sample.FilterMilliseconds, ($sample.FilterAllocatedBytes / 1MB), ($sample.ProcessPeakWorkingSetBytes / 1MB))
            }
            finally {
                if (-not $process.HasExited) {
                    $process.Kill($true)
                    $process.WaitForExit()
                }
                $process.Dispose()
            }
        }
    }
}

if ($descriptionHash -and (Get-FileHash -LiteralPath $DescriptionPath -Algorithm SHA256).Hash -ne $descriptionHash) {
    throw 'The input description changed during measurement; discard these samples and retry.'
}
$report = [ordered]@{
    Configuration = 'Release'
    CoordinatorRuntime = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    OS = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    Architecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
    DescriptionSha256 = $descriptionHash
    TimeoutSeconds = $TimeoutSeconds
    MaxWorkingSetMiB = $MaxWorkingSetMiB
    Samples = @($samples)
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath
Write-Host "Resource report: $OutputPath"
