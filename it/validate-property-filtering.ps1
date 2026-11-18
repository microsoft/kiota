#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$KiotaExecutable,
    [ValidateSet('CSharp', 'TypeScript')][string[]]$Languages = @('CSharp', 'TypeScript'),
    [string]$TypeScriptNodeModules = (Join-Path $PSScriptRoot 'typescript' -AdditionalChildPath 'node_modules')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$KiotaExecutable = (Resolve-Path -LiteralPath $KiotaExecutable).Path
$description = Join-Path $root 'tests' -AdditionalChildPath 'Kiota.Builder.IntegrationTests', 'PropertyFiltering.yaml'
$fixtures = Join-Path $PSScriptRoot 'property-filtering'
$workingDirectory = [System.IO.Directory]::CreateTempSubdirectory('kiota-property-filtering-').FullName
$tutorial = $env:KIOTA_TUTORIAL_ENABLED
$env:KIOTA_TUTORIAL_ENABLED = 'false'
try {
    foreach ($language in $Languages) {
        $target = Join-Path $workingDirectory $language
        $client = Join-Path $target 'client'
        & $KiotaExecutable generate --language $language --openapi $description --output $client `
            --namespace-name PropertyFiltering.Client --include-property 'Base#marker' `
            --include-property 'Widget#name,friends,choice' --include-property 'OptionalChoice#name' `
            --include-property 'RequiredChoice#keep' --log-level Critical
        if ($LASTEXITCODE -ne 0) { throw "$language property-filter generation failed." }
        if ($language -eq 'CSharp') {
            # Reuse the maintained integration dependency manifest rather than introducing another package version list.
            Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'csharp' -AdditionalChildPath 'basic', 'basic.csproj') -Destination $target
            Copy-Item -LiteralPath (Join-Path $fixtures 'SerializationTests.cs') -Destination $target
            & dotnet run --project (Join-Path $target 'basic.csproj') --configuration Release -- -noLogo
            if ($LASTEXITCODE -ne 0) { throw 'Generated C# compilation or serialization checks failed.' }
        }
        else {
            if (-not (Test-Path -LiteralPath $TypeScriptNodeModules -PathType Container)) { throw 'TypeScript dependencies are missing. Run npm ci --prefix it/typescript.' }
            $nodeModules = (Resolve-Path -LiteralPath $TypeScriptNodeModules).Path
            $compiler = Join-Path $nodeModules 'typescript' -AdditionalChildPath 'bin', 'tsc'
            if (-not (Test-Path -LiteralPath $compiler)) { throw 'TypeScript dependencies are missing. Run npm ci --prefix it/typescript.' }
            Copy-Item -LiteralPath (Join-Path $fixtures 'serialization.ts') -Destination $target
            @{
                compilerOptions = @{
                    target = 'ES2022'
                    module = 'ESNext'
                    moduleResolution = 'Bundler'
                    strict = $true
                    noEmit = $true
                    skipLibCheck = $true
                    types = @('node')
                    typeRoots = @((Join-Path $nodeModules '@types'))
                    paths = @{ '*' = @((Join-Path $nodeModules '*')) }
                }
                include = @('**/*.ts')
            } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $target 'tsconfig.json')
            & node $compiler --project (Join-Path $target 'tsconfig.json')
            if ($LASTEXITCODE -ne 0) { throw 'Generated TypeScript type checking failed.' }
            & node (Join-Path $fixtures 'build-typescript.cjs') $target $nodeModules
            if ($LASTEXITCODE -ne 0) { throw 'Bundling generated TypeScript failed.' }
            & node (Join-Path $target 'serialization.cjs')
            if ($LASTEXITCODE -ne 0) { throw 'Generated TypeScript serialization checks failed.' }
        }
    }
}
finally {
    $env:KIOTA_TUTORIAL_ENABLED = $tutorial
    Remove-Item -LiteralPath $workingDirectory -Recurse -Force
}
