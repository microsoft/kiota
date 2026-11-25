#Requires -Version 7.4
<#
.SYNOPSIS
    Building blocks for the scripts in .devcontainer/: a pass/fail report, the toolchain check against the
    versions this repository pins for CI, and the build and test steps, run with the repository's own
    commands and it/ scripts. A script imports this module and ends with
    `$code = 1; try { $code = Invoke-Check '<title>' { <the steps it needs> } } finally { exit $code }`,
    so that it exits with 1 when it is stopped early, too.
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '', Justification = 'Colored console report.')]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:Root = Split-Path $PSScriptRoot -Parent
$script:Results = [System.Collections.Generic.List[pscustomobject]]::new()

#region Report
function Write-Result([string] $Name, [bool] $Passed, [string] $Detail = '') {
    $script:Results.Add([pscustomobject]@{ Check = $Name; Passed = $Passed; Detail = $Detail })
    $status = $Passed ? "$($PSStyle.Foreground.Green)ok  " : "$($PSStyle.Foreground.Red)FAIL"
    Write-Host ('{0}{1} {2,-28} {3}' -f $status, $PSStyle.Reset, $Name, $Detail)
}

function Write-Section([string] $Name) { Write-Host "$($PSStyle.Foreground.Cyan)== $Name$($PSStyle.Reset)" }

# Runs a step, shows its output, and fails it on a non-zero exit code, a PowerShell error or a missing command
# (the it/*.ps1 scripts report success when a command they call is not installed). PowerShell errors are read
# from the output rather than stopped on: the step's script block runs under the preference of the scope that
# wrote it, not this one's. What native commands write to stderr arrives as NativeCommandError(Message) and is only shown.
function Invoke-Step([string] $Name, [scriptblock] $Script, [string] $Path = $script:Root) {
    Write-Section $Name
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    Push-Location $Path
    try {
        $ErrorActionPreference = 'Continue'
        $global:LASTEXITCODE = 0
        $output = $null  # stays unset when the step prints nothing
        & $Script 2>&1 | Tee-Object -Variable output | Out-Host
        $errors = @($output | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] -and $_.FullyQualifiedErrorId -notlike 'NativeCommandError*' })
        $missing = ($output | Out-String) -match 'is not recognized as a name of a cmdlet|command not found'
        $problem = $errors ? ", $($errors[0])" : ($missing ? ', a command was missing' : '')
        Write-Result $Name ($LASTEXITCODE -eq 0 -and -not $errors -and -not $missing) ('exit {0} in {1:n0}s{2}' -f $LASTEXITCODE, $timer.Elapsed.TotalSeconds, $problem)
    }
    catch { Write-Result $Name $false $_.Exception.Message }
    finally { Pop-Location }
}

# Runs $Body, then prints the summary and returns the exit code: 0 only when $Body ran to its end, recorded
# at least one check and every check passed. An exception, or a break or continue that would otherwise end
# the script early with exit code 0, is recorded as a failure.
function Invoke-Check([string] $Title, [scriptblock] $Body) {
    $finished = $false
    try {
        # Out-Host: whatever $Body writes is shown, and never becomes part of the exit code returned here.
        do { & $Body | Out-Host; $finished = $true } while ($false)
        if (-not $finished) { Write-Result 'ran to the end' $false 'a break or continue left the checks early' }
    }
    catch { Write-Result 'ran to the end' $false "$($_.Exception.Message) ($($_.InvocationInfo.PositionMessage -replace '\s+', ' '))" }
    if ($script:Results.Count -eq 0) { Write-Result 'checks recorded' $false 'nothing was checked' }

    $failed = @($script:Results | Where-Object { -not $_.Passed })
    $summary = '{0} checks, {1} failed ({2}, {3})' -f $script:Results.Count, $failed.Count, $Title, [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    Write-Host "`n$($failed ? $PSStyle.Foreground.Red : $PSStyle.Foreground.Green)$summary$($PSStyle.Reset)"
    $failed | ForEach-Object { Write-Host "FAIL $($_.Check): $($_.Detail)" }
    $failed ? 1 : 0
}
#endregion

#region Toolchain: presence and versions against the CI pins
# The first capture group of $Pattern in a repository file: how the repository pins a version for CI.
function Get-Pin([string] $Path, [string] $Pattern) {
    $match = Select-String -Path (Join-Path $script:Root $Path) -Pattern $Pattern | Select-Object -First 1
    if (-not $match) { throw "No match for '$Pattern' in $Path; update $PSCommandPath to follow that file." }
    $match.Matches[0].Groups[1].Value
}

# The version a tool reports, or $null when the tool is missing or fails (a failing `dotnet --version`
# prints the SDK version global.json requests, which must not count as installed).
function Get-ToolVersion([string[]] $CommandLine, [string] $Pattern = '(\d+\.\d+(?:\.\d+){0,2})') {
    $command, $arguments = $CommandLine[0], @($CommandLine | Select-Object -Skip 1)
    if (-not (Get-Command $command -CommandType Application -ErrorAction Ignore)) { return $null }
    $ErrorActionPreference = 'Continue'  # several tools print their version on stderr
    $output = & $command @arguments 2>&1 | Out-String
    $LASTEXITCODE -eq 0 -and $output -match $Pattern ? [version]$Matches[1] : $null
}

# True when $Version starts with $Pin ("17" matches 17.0.20, "3.14" matches 3.14.10).
function Test-Pin([version] $Version, [string] $Pin) {
    $parts = $Pin.Split('.')
    $Version.Major -eq $parts[0] -and ($parts.Count -eq 1 -or $Version.Minor -eq $parts[1])
}

# Checks every tool against the versions the repository pins for CI (global.json, it/go/go.mod,
# it/dart/pubspec.yaml, .github/workflows/*.yml).
function Test-Toolchain {
    Push-Location $script:Root
    try {
        $ci = '.github/workflows/integration-tests.yml'
        $sdk = [version](Get-Content global.json -Raw | ConvertFrom-Json).sdk.version
        $pins = @{
            Java    = Get-Pin $ci 'java-version:\s*"?(\d+)'
            Node    = Get-Pin $ci 'node-version:\s*"?(\d+)'
            NodeExt = Get-Pin .github/workflows/build-vscode-extension.yml 'node-version:\s*"?(\d+)'
            GoCaret = [version](Get-Pin $ci 'go-version:\s*"?\^(\d+\.\d+)')
            GoMod   = [version](Get-Pin it/go/go.mod '^go\s+(\d+\.\d+(?:\.\d+)?)')
            Python  = Get-Pin $ci 'python-version:\s*"?(\d+\.\d+)'
            Ruby    = Get-Pin $ci 'ruby-version:\s*"?(\d+\.\d+)'
            Php     = Get-Pin $ci 'php-version:\s*"?(\d+\.\d+)'
            Dart    = [version](Get-Pin it/dart/pubspec.yaml '^\s+sdk:\s*"?\^(\d+\.\d+(?:\.\d+)?)')
        }

        # Name, command line that prints the version, what CI expects, and the rule the version must meet.
        # dotnet --version runs in the repository root, so it fails outright when no SDK satisfies global.json.
        $tools = @(
            @{ Name = '.NET SDK'; Cmd = 'dotnet', '--version'; Expect = ">= $sdk within $($sdk.ToString(2)) (global.json); if older: Rebuild Without Cache"; Rule = { param($v) (Test-Pin $v $sdk.ToString(2)) -and $v -ge $sdk } }
            @{ Name = 'Java'; Cmd = 'java', '-version'; Expect = "$($pins.Java) ($ci)"; Rule = { param($v) Test-Pin $v $pins.Java } }
            @{ Name = 'Maven'; Cmd = 'mvn', '-v'; Expect = 'any (MockServer, Java ITs)'; Rule = { $true } }
            @{ Name = 'Node.js'; Cmd = 'node', '--version'; Expect = "$($pins.Node) (integration tests), $($pins.NodeExt) (extension)"; Rule = { param($v) (Test-Pin $v $pins.Node) -and (Test-Pin $v $pins.NodeExt) } }
            @{ Name = 'Go'; Cmd = 'go', 'version'; Expect = "^$($pins.GoCaret) ($ci), >= $($pins.GoMod) (it/go/go.mod)"; Rule = { param($v) $v.Major -eq $pins.GoCaret.Major -and $v -ge $pins.GoCaret -and $v -ge $pins.GoMod } }
            @{ Name = 'Python'; Cmd = 'python3', '--version'; Expect = "$($pins.Python) ($ci)"; Rule = { param($v) Test-Pin $v $pins.Python } }
            @{ Name = 'Ruby'; Cmd = 'ruby', '--version'; Expect = "$($pins.Ruby) ($ci)"; Rule = { param($v) Test-Pin $v $pins.Ruby } }
            @{ Name = 'Bundler'; Cmd = 'bundle', '--version'; Expect = 'any (Ruby ITs)'; Rule = { $true } }
            @{ Name = 'PHP'; Cmd = 'php', '--version'; Expect = "$($pins.Php) ($ci)"; Rule = { param($v) Test-Pin $v $pins.Php } }
            @{ Name = 'Composer'; Cmd = 'composer', '--version', '--no-interaction'; Pattern = 'Composer version (\d+\.\d+\.\d+)'; Expect = 'any (PHP ITs)'; Rule = { $true } }
            @{ Name = 'Dart'; Cmd = 'dart', '--version'; Expect = "^$($pins.Dart) (it/dart/pubspec.yaml)"; Rule = { param($v) $v.Major -eq $pins.Dart.Major -and $v -ge $pins.Dart } }
            @{ Name = 'PowerShell'; Cmd = 'pwsh', '--version'; Expect = '7.x (it/*.ps1)'; Rule = { param($v) $v.Major -eq 7 } }
            @{ Name = 'GitHub CLI'; Cmd = 'gh', '--version'; Expect = 'any'; Rule = { $true } }
            @{ Name = 'reportgenerator'; Cmd = 'dotnet', 'tool', 'list', '--global'; Pattern = 'dotnet-reportgenerator-globaltool\s+(\d+\.\d+\.\d+)'; Expect = 'any (coverage reports, as in dotnet.yml)'; Rule = { $true } }
            @{ Name = 'dotnet-outdated'; Cmd = 'dotnet', 'tool', 'list', '--global'; Pattern = 'dotnet-outdated-tool\s+(\d+\.\d+\.\d+)'; Expect = 'any (dotnet outdated)'; Rule = { $true } }
        )

        Write-Section 'Tools'
        foreach ($tool in $tools) {
            $version = $tool.ContainsKey('Pattern') ? (Get-ToolVersion $tool.Cmd $tool.Pattern) : (Get-ToolVersion $tool.Cmd)
            Write-Result $tool.Name ($null -ne $version -and (& $tool.Rule $version)) "$($version ?? 'missing'), expects $($tool.Expect)"
        }
        # Missing tools count as failures, like the version checks above, instead of stopping the script.
        if ($python = Get-Command python3 -CommandType Application -ErrorAction Ignore) {
            $pipBlocked = python3 -c 'import os, sysconfig; print(os.path.exists(os.path.join(sysconfig.get_path("stdlib"), "EXTERNALLY-MANAGED")))'
            Write-Result 'pip install allowed' ($pipBlocked -eq 'False') "python3 is $($python.Source) (not the PEP 668 system Python)"
        }
        else { Write-Result 'pip install allowed' $false 'python3 missing' }
        $xvfb = [bool](Get-Command xvfb-run -CommandType Application -ErrorAction Ignore)
        if ($xvfb) { $null = xvfb-run -a true 2>&1; $xvfb = $LASTEXITCODE -eq 0 }
        Write-Result 'xvfb-run' $xvfb 'headless display for the extension tests'
    }
    finally { Pop-Location }
}
#endregion

#region Build and test steps, as the workflows run them
# The newest framework kiota targets, which CI publishes.
function Get-KiotaFramework {
    (Select-Xml -Path (Join-Path $script:Root src/kiota/kiota.csproj) -XPath '//TargetFrameworks').Node.InnerText.Split(';', [StringSplitOptions]::RemoveEmptyEntries) |
        Sort-Object { [version]($_ -replace '^net') } | Select-Object -Last 1
}

function Invoke-Build {
    Invoke-Step 'dotnet build' { dotnet build kiota.slnx }
    # As build-vscode-extension.yml does: node_modules from when the container was created may predate the lock file.
    Invoke-Step 'npm ci' { npm ci } (Join-Path $script:Root vscode)
    Invoke-Step 'extension build' { npm run build } (Join-Path $script:Root vscode)
}

function Invoke-DotnetTest { Invoke-Step 'dotnet test' { dotnet test kiota.slnx --no-build } }

function Invoke-ExtensionTest {
    $framework = Get-KiotaFramework
    $rid = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64' ? 'linux-arm64' : 'linux-x64'
    $vscode = Join-Path $script:Root vscode

    # The npm package and extension tests download the kiota release named in lib/runtime.json (CI points it
    # at the latest release first). Hand them the kiota built from this checkout instead: the npm package
    # tests find it in lib/.kiotabin/<version>/<rid>/, the extension tests through the sideloading hook in
    # vscode/packages/npm-package/lib/install.ts. The npm package tests are not sideloaded because one of
    # them checks that a download with a bad hash is rejected.
    $npmPackage = Join-Path $vscode packages/npm-package
    $kiotaVersion = (Get-Content "$npmPackage/lib/runtime.json" -Raw | ConvertFrom-Json).kiotaVersion
    # install.ts uses whatever binary a .kiotabin already holds and only unpacks one into an empty directory, so a
    # .kiotabin left from an earlier run, the npm package's or the extension's own (it installs kiota next to
    # itself), would be tested instead of the checkout's kiota. Rename each aside into vscode/.kiotabin-aside
    # until the tests are done: on the same file system a move is a rename that either happens or not. Creating
    # that directory also takes the lock for these tests: mkdir fails if it exists, because another run is using
    # it or one was interrupted there; stop then rather than guess which binaries are the developer's.
    # (dist/.kiotabin is not among them: dist/ is build output, which every build of the npm package empties.)
    $binDirs = 'packages/npm-package/lib/.kiotabin', 'packages/npm-package/.kiotabin', 'packages/microsoft-kiota/.kiotabin'
    $asideRoot = Join-Path $vscode .kiotabin-aside
    & (Get-Command mkdir -CommandType Application)[0].Source $asideRoot 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Result '.kiotabin set aside' $false "$asideRoot exists: another run is using it, or an interrupted run left the .kiotabin directories it found in it; then, for each, delete vscode/<its name, + read as /> and move it back there, and remove $asideRoot"
        return
    }
    $temp = [System.IO.Directory]::CreateTempSubdirectory('kiota-').FullName  # never files of an earlier publish
    $published, $zip = (Join-Path $temp $rid), (Join-Path $temp "$rid.zip")
    $existing = @($binDirs | Where-Object { Test-Path -LiteralPath (Join-Path $vscode $_) })
    $aside = [System.Collections.Generic.List[string]]::new()
    try {
        foreach ($dir in $existing) {
            Move-Item -LiteralPath (Join-Path $vscode $dir) (Join-Path $asideRoot ($dir -replace '/', '+'))
            $aside.Add($dir)
        }
        Invoke-Step 'publish kiota (extension tests)' {
            dotnet publish src/kiota/kiota.csproj -c Release -r $rid --self-contained -p:PublishSingleFile=true -f $framework -o $published
            Compress-Archive -Path "$published/*" -DestinationPath $zip
            $libBin = "$npmPackage/lib/.kiotabin/$kiotaVersion/$rid"
            New-Item -ItemType Directory -Path (Split-Path $libBin) -Force | Out-Null
            Copy-Item $published $libBin -Recurse
        }
        Invoke-Step 'extension lint' { npm run lint } $vscode
        Invoke-Step 'npm package tests' { npm run test:package } $vscode
        Invoke-Step 'extension tests' { $env:KIOTA_SIDELOADING_BINARY_ZIP_PATH = $zip; xvfb-run -a npm run test:vscode } $vscode
    }
    finally {
        Remove-Item Env:KIOTA_SIDELOADING_BINARY_ZIP_PATH -ErrorAction Ignore
        Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction Ignore
        # Remove what the tests created, never a directory that is still in place because moving it aside failed.
        $binDirs | Where-Object { $_ -notin $existing -or $_ -in $aside } | ForEach-Object { Remove-Item -LiteralPath (Join-Path $vscode $_) -Recurse -Force -ErrorAction Ignore }
        foreach ($dir in $aside) { Move-Item -LiteralPath (Join-Path $asideRoot ($dir -replace '/', '+')) (Join-Path $vscode $dir) }
        [System.IO.Directory]::Delete($asideRoot)  # releases the lock; throws unless everything went back
    }
}

# Runs one of the repository's scripts as a GitHub Actions `shell: pwsh` step does: in a new pwsh process in the
# repository root (or another checkout of it), without strict mode, with $ErrorActionPreference = 'stop' set first, and exiting with the last
# command's exit code. The it/ scripts depend on all of it: they read properties that may not exist;
# it/exec-cmd.ps1's Invoke-Call stops at a failing command only under 'stop' (otherwise a later command, such as
# stopping MockServer, resets the exit code to 0); and when it stops, it exits without undoing its
# Push-Location, which would move every later step to another directory if the script shared this process.
function Invoke-RepositoryScript([string] $Path, [hashtable] $Parameters = @{}, [string] $WorkingDirectory = $script:Root) {
    $quote = { "'{0}'" -f ($args[0] -replace "'", "''") }
    $arguments = $Parameters.GetEnumerator() | ForEach-Object { "-$($_.Key) $(& $quote $_.Value)" }
    $command = "`$ErrorActionPreference = 'stop'; & $(& $quote $Path) $arguments; if (Test-Path -LiteralPath variable:\LASTEXITCODE) { exit `$LASTEXITCODE }"
    pwsh -NoProfile -NonInteractive -WorkingDirectory $WorkingDirectory -Command $command
}

# Generates and tests a client for the description in each language with it/generate-code.ps1 and
# it/exec-cmd.ps1, as integration-tests.yml does, against the kiota published from this checkout.
function Invoke-IntegrationTest([string] $Description, [string[]] $Language) {
    Push-Location $script:Root
    try {
        # The it/ scripts regenerate the clients under it/ and delete or rewrite tracked files there, such as
        # it/ruby/Gemfile.lock. They run in a temporary worktree instead, so nothing under the developer's it/
        # changes. The worktree holds HEAD plus the developer's uncommitted changes to tracked files (git stash
        # create commits them without touching anything), not untracked files: stop while it/ or a description in
        # the repository has any change, so what runs is exactly what the developer sees there.
        $paths = @('it/') + @($Description.StartsWith('./') ? $Description : @())
        $changes = @(git status --porcelain --untracked-files=all -- @paths)
        if ($LASTEXITCODE -ne 0 -or $changes) {
            Write-Result "$($paths -join ', ') as committed" $false ($changes ? "$($changes.Count) changed; commit or stash them first, the integration tests run in a separate worktree" : "git status failed (exit $LASTEXITCODE)")
            return
        }
        $tree = [System.IO.Directory]::CreateTempSubdirectory('kiota-it-').FullName
        $commit = git stash create
        if ($LASTEXITCODE -eq 0) { git -c core.hooksPath=/dev/null worktree add --quiet --detach $tree ($commit ?? 'HEAD') }
        if ($LASTEXITCODE -ne 0) {
            Write-Result 'integration test worktree' $false "git stash create or worktree add $tree failed (exit $LASTEXITCODE)"
            Remove-Item -LiteralPath $tree -Recurse -Force -ErrorAction Ignore
            return
        }
        try {
            $framework = Get-KiotaFramework
            Invoke-Step 'publish kiota (integration tests)' { dotnet publish src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -f $framework -o (Join-Path $tree publish) }
            foreach ($lang in $Language.ToLowerInvariant()) {  # the it/ scripts use it as a directory name
                Invoke-Step "integration test: $lang" {
                    Invoke-RepositoryScript ./it/generate-code.ps1 @{ descriptionUrl = $Description; descriptionKey = $Description; language = $lang } $tree
                    if ($LASTEXITCODE -eq 0) { Invoke-RepositoryScript ./it/exec-cmd.ps1 @{ descriptionUrl = $Description; language = $lang } $tree }
                }
                $null = Invoke-RepositoryScript ./it/do-clean.ps1 @{ language = $lang } $tree 2>&1
                if ($LASTEXITCODE -ne 0) { Write-Result "clean up: $lang" $false "it/do-clean.ps1 exited with $LASTEXITCODE" }
            }
        }
        finally {
            # exec-cmd.ps1 stops MockServer at its end, which it never reaches when a test fails; stop it here too,
            # or its JVM keeps port 1080 after the worktree it runs from is gone.
            if (Get-Command mvn -CommandType Application -ErrorAction Ignore) {
                Push-Location (Join-Path $tree it/mockserver)
                try { $null = mvn --batch-mode --quiet mockserver:stopForked 2>&1 }
                finally { Pop-Location }
            }
            git worktree remove --force $tree
            if ($LASTEXITCODE -ne 0) { Write-Result 'integration test worktree' $false "git worktree remove $tree failed (exit $LASTEXITCODE)" }
        }
    }
    finally { Pop-Location }
}
#endregion

Export-ModuleMember -Function Write-Result, Invoke-Step, Invoke-Check, Test-Toolchain, Invoke-RepositoryScript,
    Invoke-Build, Invoke-DotnetTest, Invoke-ExtensionTest, Invoke-IntegrationTest
