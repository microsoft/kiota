#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.0' }
# Tests for DevContainer.psm1: Invoke-Pester .devcontainer
# Each case runs in a new pwsh process, as the scripts do, so exit codes and early exits are real.

BeforeAll {
    $module = Join-Path $PSScriptRoot DevContainer.psm1

    # Runs $Body inside Invoke-Check in a new process; returns its exit code and its output without colors.
    function Invoke-CheckProcess([string] $Body, [string] $Module = $module) {
        $output = pwsh -NoProfile -NonInteractive -Command "Import-Module '$Module' -Force; exit (Invoke-Check 'test' { $Body })" 2>&1
        [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output | Out-String) -replace '\e\[[0-9;]*m' }
    }
}

Describe 'Invoke-Check' {
    It 'returns 0 when every check passed' {
        (Invoke-CheckProcess 'Write-Result a $true; Write-Result b $true').ExitCode | Should -Be 0
    }

    It 'returns 1 and names the check that failed' {
        $run = Invoke-CheckProcess 'Write-Result a $true; Write-Result b $false "why"'
        $run.ExitCode | Should -Be 1
        $run.Output | Should -Match 'FAIL b: why'
    }

    It 'fails when nothing was checked' {
        $run = Invoke-CheckProcess '$null = 1'
        $run.ExitCode | Should -Be 1
        $run.Output | Should -Match 'FAIL checks recorded'
    }

    It 'fails on an exception and reports it' {
        $run = Invoke-CheckProcess 'Write-Result a $true; throw "boom"'
        $run.ExitCode | Should -Be 1
        $run.Output | Should -Match 'FAIL ran to the end: boom'
    }

    It 'fails when <Case> ends the checks early' -ForEach @(
        @{ Case = 'a break in ForEach-Object'; Body = 'Write-Result a $true; 1..3 | ForEach-Object { break }; Write-Result b $true' }
        @{ Case = 'a continue in ForEach-Object'; Body = 'Write-Result a $true; 1..3 | ForEach-Object { continue }' }
        @{ Case = 'a break in a nested function'; Body = 'function f { 1 | ForEach-Object { break } }; Write-Result a $true; f' }
    ) {
        $run = Invoke-CheckProcess $Body
        $run.ExitCode | Should -Be 1
        $run.Output | Should -Match 'FAIL ran to the end: a break or continue'
    }

    It 'returns 0 when the checks write output of their own' {
        (Invoke-CheckProcess 'Write-Result a $true; 0; "text"; 1').ExitCode | Should -Be 0
    }
}

Describe 'Invoke-Step' {
    It 'passes a command that succeeds, with or without output' {
        (Invoke-CheckProcess 'Invoke-Step quiet { bash -c "exit 0" }; Invoke-Step loud { bash -c "echo hi" }').ExitCode | Should -Be 0
    }

    It 'passes a command that writes to stderr and succeeds' {
        (Invoke-CheckProcess 'Invoke-Step s { bash -c "echo warning >&2; echo more >&2; exit 0" }').ExitCode | Should -Be 0
    }

    It 'passes a step without native commands after one that failed outside it' {
        (Invoke-CheckProcess 'bash -c "exit 1"; Invoke-Step s { $null = 1 }').ExitCode | Should -Be 0
    }

    It 'fails <Case>' -ForEach @(
        @{ Case = 'a command with a non-zero exit code'; Body = 'Invoke-Step s { bash -c "exit 3" }'; Reason = 'FAIL s: exit 3' }
        @{ Case = 'a command that is not installed'; Body = 'Invoke-Step s { not-a-command-xyz }'; Reason = 'FAIL s: .*not-a-command-xyz' }
        @{ Case = 'a step that throws'; Body = 'Invoke-Step s { throw "inner" }'; Reason = 'FAIL s: inner' }
        @{ Case = 'a script that hides a missing command'; Body = 'Invoke-Step s { pwsh -NoProfile -Command "not-a-command-xyz; exit 0" }'; Reason = 'FAIL s: .*a command was missing' }
        @{ Case = 'a PowerShell error after a command that succeeded'; Body = 'Invoke-Step s { bash -c "exit 0"; Write-Error boom }'; Reason = 'FAIL s: .*boom' }
        @{ Case = 'a cmdlet that fails'; Body = 'Invoke-Step s { Copy-Item /not-a-path-xyz/a /tmp }'; Reason = 'FAIL s: .*not-a-path-xyz' }
    ) {
        $run = Invoke-CheckProcess $Body
        $run.ExitCode | Should -Be 1
        $run.Output | Should -Match $Reason
    }

    It 'keeps running the next steps after a failure' {
        $run = Invoke-CheckProcess 'Invoke-Step first { bash -c "exit 1" }; Invoke-Step second { bash -c "exit 0" }'
        $run.ExitCode | Should -Be 1
        $run.Output | Should -Match 'ok   second'
    }
}

Describe 'Invoke-RepositoryScript' {
    BeforeAll {
        # Behaves like it/exec-cmd.ps1 for Ruby: Invoke-Call stops at a failing command only under 'Stop', and then
        # exits without undoing its Push-Location; the last command (stopping MockServer) succeeds.
        $likeExecCmd = Join-Path $TestDrive like-exec-cmd.ps1
        Set-Content $likeExecCmd @'
function Invoke-Call { param([scriptblock] $ScriptBlock, [string] $ErrorAction = $ErrorActionPreference); & $ScriptBlock; if ($LASTEXITCODE -ne 0 -and $ErrorAction -eq 'Stop') { exit $LASTEXITCODE } }
Push-Location $PSScriptRoot
$env:FROM_SCRIPT = 'leaked'
Invoke-Call { bash -c 'exit 1' }
bash -c 'exit 0'
Pop-Location
'@
        $nonStrict = Join-Path $TestDrive non-strict.ps1
        Set-Content $nonStrict '$o = [pscustomobject]@{}; if ($o.missing) { exit 1 }'
        $echo = Join-Path $TestDrive echo.ps1
        Set-Content $echo 'param([string] $Text) Set-Content (Join-Path $PSScriptRoot echoed.txt) $Text -NoNewline'
    }

    It 'fails the step when a script stops at a failing command, as a GitHub Actions pwsh step would' {
        $run = Invoke-CheckProcess "Invoke-Step s { Invoke-RepositoryScript '$likeExecCmd' }"
        $run.ExitCode | Should -Be 1
        $run.Output | Should -Match 'FAIL s: exit 1'
    }

    It 'keeps the location and environment of the caller when a script exits early' {
        $run = Invoke-CheckProcess "Set-Location /; Invoke-Step s { Invoke-RepositoryScript '$likeExecCmd' }; Write-Result location (`$PWD.Path -eq '/') `$PWD.Path; Write-Result environment (-not `$env:FROM_SCRIPT) ''"
        $run.Output | Should -Match 'ok   location'
        $run.Output | Should -Match 'ok   environment'
    }

    It 'runs a script without strict mode, as CI does' {
        (Invoke-CheckProcess "Invoke-Step s { Invoke-RepositoryScript '$nonStrict' }").ExitCode | Should -Be 0
    }

    It 'passes parameters verbatim' {
        (Invoke-CheckProcess "Invoke-RepositoryScript '$echo' @{ Text = 'it''s `$HOME x' }; Write-Result done `$true").ExitCode | Should -Be 0
        Get-Content (Join-Path $TestDrive echoed.txt) -Raw | Should -BeExactly 'it''s $HOME x'
    }
}

# Against a scratch worktree whose it/ scripts and tools are stand-ins, so the control flow runs in seconds.
Describe 'Integration and extension tests in a scratch worktree' {
    BeforeAll {
        $repo = Join-Path $TestDrive repo
        git -C $PSScriptRoot worktree add --quiet --detach $repo HEAD
        $repoModule = Join-Path $repo .devcontainer/DevContainer.psm1
        Copy-Item $module $repoModule -Force
        $it = Join-Path $repo it
        # generate-code changes a tracked file and adds one; exec-cmd fails for language 'bad' the way the real one
        # does for Ruby (see above).
        Set-Content $it/generate-code.ps1 @'
param($descriptionUrl, $descriptionKey, $language)
if ($language -eq 'local' -and -not (Select-String -Quiet -SimpleMatch uncommitted-marker (Join-Path $PSScriptRoot ../README.md))) { exit 1 }
Add-Content (Join-Path $PSScriptRoot config.json) ' '
Set-Content (Join-Path $PSScriptRoot "generated-$language.txt") x
'@
        Set-Content $it/exec-cmd.ps1 @'
param($descriptionUrl, $language)
function Invoke-Call { param([scriptblock] $ScriptBlock, [string] $ErrorAction = $ErrorActionPreference); & $ScriptBlock; if ($LASTEXITCODE -ne 0 -and $ErrorAction -eq 'Stop') { exit $LASTEXITCODE } }
Push-Location $PSScriptRoot
if ($language -cne $language.ToLowerInvariant()) { exit 2 }  # it/<language> is a directory, and Linux is case-sensitive
Invoke-Call { bash -c "exit $($language -eq 'bad' ? 1 : 0)" }
bash -c 'exit 0'
Pop-Location
'@
        Set-Content $it/do-clean.ps1 'param($language) if ($language -eq ''bad'') { exit 3 }'
        git -C $repo -c user.name=test -c user.email=test@example.invalid commit --quiet -am 'stand-in it/ scripts'

        # Stand-in tools. dotnet publish writes a kiota that says "fresh <rid>". The npm package tests empty dist/,
        # as their build does, and fail unless lib/.kiotabin holds that kiota for this machine and no stale one is
        # in the package; the extension tests fail without the sideloaded zip or with a stale kiota of their own.
        $bin = Join-Path $TestDrive bin
        New-Item -ItemType Directory $bin | Out-Null
        Set-Content $bin/dotnet @'
#!/bin/sh
while [ $# -gt 0 ]; do
  case "$1" in -r) rid=$2 ;; -o) out=$2 ;; esac
  shift
done
mkdir -p "$out" && echo "fresh $rid" > "$out/kiota"
'@
        Set-Content $bin/npm @'
#!/bin/sh
rid=$([ "$(uname -m)" = aarch64 ] && echo linux-arm64 || echo linux-x64)
case "$2" in
  test:package)
    rm -rf packages/npm-package/dist
    grep -qs "fresh $rid" packages/npm-package/lib/.kiotabin/*/$rid/kiota || { echo 'not the kiota just published'; exit 1; }
    ! grep -rqs stale packages/npm-package/.kiotabin || { echo 'a stale kiota in the package'; exit 1; } ;;
  test:vscode)
    [ -f "$KIOTA_SIDELOADING_BINARY_ZIP_PATH" ] || { echo 'no sideloaded kiota'; exit 1; }
    ! grep -rqs stale packages/microsoft-kiota/.kiotabin || { echo 'a stale kiota in the extension'; exit 1; } ;;
esac
'@
        Set-Content $bin/xvfb-run "#!/bin/sh`nshift`nexec `"`$@`""
        $mvnCalls = Join-Path $TestDrive mvn-calls.txt
        Set-Content $bin/mvn "#!/bin/sh`necho `"`$*`" >> '$mvnCalls'"
        chmod +x $bin/dotnet $bin/npm $bin/xvfb-run $bin/mvn
        $usingStandIns = "`$env:PATH = '$bin' + [IO.Path]::PathSeparator + `$env:PATH;"
    }

    AfterAll { git -C $PSScriptRoot worktree remove --force $repo }

    It 'refuses to start the integration tests while it/ has local changes, and keeps them' {
        Add-Content $it/config.json '// mine'
        Set-Content $it/mine.txt 'mine'
        try {
            $run = Invoke-CheckProcess "$usingStandIns Invoke-IntegrationTest -Description d -Language good" $repoModule
            $run.ExitCode | Should -Be 1
            $run.Output | Should -Match 'FAIL it/ as committed: 2 changed'
            $run.Output | Should -Not -Match 'publish kiota'
            Get-Content $it/config.json -Raw | Should -Match '// mine'
            "$it/mine.txt" | Should -Exist
        }
        finally { git -C $repo checkout --quiet -- it; git -C $repo clean -fdq -- it }
    }

    It 'refuses to start the integration tests when git status fails' {
        $copy = Join-Path $TestDrive not-a-repo/.devcontainer
        New-Item -ItemType Directory $copy | Out-Null
        Copy-Item $module $copy
        $run = Invoke-CheckProcess "Invoke-IntegrationTest -Description d -Language good" (Join-Path $copy DevContainer.psm1)
        $run.ExitCode | Should -Be 1
        $run.Output | Should -Match 'FAIL it/ as committed: git status failed'
    }

    It 'refuses to start the integration tests while the description has local changes' {
        $description = './tests/Kiota.Builder.IntegrationTests/InheritingErrors.yaml'
        Add-Content (Join-Path $repo $description) '# mine'
        try {
            $run = Invoke-CheckProcess "$usingStandIns Invoke-IntegrationTest -Description '$description' -Language good" $repoModule
            $run.ExitCode | Should -Be 1
            $run.Output | Should -Match 'FAIL it/, ./tests/Kiota.Builder.IntegrationTests/InheritingErrors.yaml as committed: 1 changed'
            $run.Output | Should -Not -Match 'publish kiota'
        }
        finally { git -C $repo checkout --quiet -- tests }
    }

    It 'runs the integration tests for a language named in another case' {
        $run = Invoke-CheckProcess "$usingStandIns Invoke-IntegrationTest -Description d -Language GOOD" $repoModule
        $run.Output | Should -Match 'ok   integration test: good'
        $run.ExitCode | Should -Be 0
    }

    It 'runs the integration tests with uncommitted changes outside it/' {
        Add-Content (Join-Path $repo README.md) 'uncommitted-marker'
        try {
            $run = Invoke-CheckProcess "$usingStandIns Invoke-IntegrationTest -Description d -Language local" $repoModule
            $run.Output | Should -Match 'ok   integration test: local'
            $run.ExitCode | Should -Be 0
        }
        finally { git -C $repo checkout --quiet -- README.md }
    }

    It 'reports a failing integration test, runs the next language and leaves the developer''s it/ alone' {
        # A generated client the developer is debugging: ignored by it/python/.gitignore, so git status can't see it.
        $debugging = Join-Path $it python/integration_test/client/debugging.py
        New-Item -ItemType File $debugging -Value 'mine' -Force | Out-Null
        $worktrees = git -C $repo worktree list
        try {
            $run = Invoke-CheckProcess "$usingStandIns Invoke-IntegrationTest -Description d -Language bad, good" $repoModule
            $run.ExitCode | Should -Be 1
            $run.Output | Should -Match 'FAIL integration test: bad'
            $run.Output | Should -Match 'ok   integration test: good'
            $run.Output | Should -Match 'FAIL clean up: bad'
            Get-Content $mvnCalls | Should -Contain '--batch-mode --quiet mockserver:stopForked'  # exec-cmd stopped early
            git -C $repo status --porcelain -- it | Should -BeNullOrEmpty
            Get-ChildItem $it -Filter 'generated-*' | Should -BeNullOrEmpty
            Get-Content $debugging -Raw | Should -BeExactly 'mine'
            git -C $repo worktree list | Should -Be $worktrees
        }
        finally { Remove-Item (Split-Path $debugging) -Recurse -Force }  # only the ignored client directory created above
    }

    It 'tests the kiota just published instead of a stale .kiotabin, and puts those back' {
        $vscode = Join-Path $repo vscode
        $version = (Get-Content $vscode/packages/npm-package/lib/runtime.json -Raw | ConvertFrom-Json).kiotaVersion
        $rid = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64' ? 'linux-arm64' : 'linux-x64'
        $stale = 'npm-package/lib', 'npm-package', 'microsoft-kiota' | ForEach-Object { "$vscode/packages/$_/.kiotabin/$version/$rid/kiota" }
        $stale | ForEach-Object { New-Item -ItemType File $_ -Value stale -Force | Out-Null }
        try {
            $run = Invoke-CheckProcess "$usingStandIns Invoke-ExtensionTest" $repoModule
            $run.Output | Should -Match 'ok   npm package tests'
            $run.Output | Should -Match 'ok   extension tests'
            $run.ExitCode | Should -Be 0
            $stale | ForEach-Object { Get-Content $_ -Raw | Should -BeExactly 'stale' }
            Get-ChildItem "$vscode/packages/npm-package/lib/.kiotabin/$version/$rid" -Name | Should -BeExactly 'kiota'
            "$vscode/.kiotabin-aside" | Should -Not -Exist
        }
        finally { $stale | ForEach-Object { Remove-Item (Split-Path (Split-Path (Split-Path $_))) -Recurse -Force -ErrorAction Ignore } }
    }

    It 'refuses to start the extension tests while an interrupted run has a .kiotabin set aside, and keeps it' {
        $waiting = Join-Path $repo 'vscode/.kiotabin-aside/packages+npm-package+lib+.kiotabin'
        New-Item -ItemType File "$waiting/mine" -Value mine -Force | Out-Null
        try {
            $run = Invoke-CheckProcess "$usingStandIns Invoke-ExtensionTest" $repoModule
            $run.ExitCode | Should -Be 1
            $run.Output | Should -Match 'FAIL .kiotabin set aside .*another run is using it, or an interrupted run'
            $run.Output | Should -Not -Match 'publish kiota'
            Get-Content "$waiting/mine" -Raw | Should -BeExactly 'mine'
        }
        finally { Remove-Item (Join-Path $repo vscode/.kiotabin-aside) -Recurse -Force }
    }
}

Describe 'Version helpers' {
    BeforeAll { Import-Module $module -Force }

    It 'matches <Version> against pin <Pin>: <Expected>' -ForEach @(
        @{ Version = '17.0.20'; Pin = '17'; Expected = $true }
        @{ Version = '3.14.10'; Pin = '3.14'; Expected = $true }
        @{ Version = '3.13.9'; Pin = '3.14'; Expected = $false }
        @{ Version = '18.0.1'; Pin = '17'; Expected = $false }
    ) {
        InModuleScope DevContainer -Parameters $_ { Test-Pin ([version]$Version) $Pin } | Should -Be $Expected
    }

    It 'reports no version for a tool that is not installed' {
        InModuleScope DevContainer { Get-ToolVersion 'not-a-command-xyz', '--version' } | Should -BeNullOrEmpty
    }

    It 'reports no version for a tool that prints one but fails' {
        $bin = Join-Path $TestDrive failing-bin
        New-Item -ItemType Directory $bin | Out-Null
        Set-Content $bin/failing-tool "#!/bin/sh`necho 10.0.404`nexit 1"
        chmod +x $bin/failing-tool
        $path = $env:PATH
        try {
            $env:PATH = $bin + [IO.Path]::PathSeparator + $env:PATH
            InModuleScope DevContainer { Get-ToolVersion 'failing-tool', '--version' } | Should -BeNullOrEmpty
        }
        finally { $env:PATH = $path }
    }

    It 'reads every CI pin the toolchain check uses from the repository' {
        # Test-Toolchain throws when a pin pattern no longer matches its file; any other outcome is a pass here.
        { InModuleScope DevContainer { Test-Toolchain 6>$null } } | Should -Not -Throw
    }
}
