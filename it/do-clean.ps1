#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)][string]$language
)

if ([string]::IsNullOrEmpty($language)) {
    Write-Error "Language is empty"
    exit 1
}

if ($language -eq "csharp") {
    Remove-Item ./it/csharp/obj -Recurse -ErrorAction SilentlyContinue
}
elseif ($language -eq "typescript") {
    Remove-Item ./it/typescript/node_modules -Recurse -ErrorAction SilentlyContinue
    Remove-Item ./it/typescript/dist -Recurse -ErrorAction SilentlyContinue
}
elseif ($language -eq "ruby") {
    Remove-Item ./it/ruby/vendor -Recurse -ErrorAction SilentlyContinue
    Remove-Item ./it/ruby/.git -Recurse -ErrorAction SilentlyContinue
    Remove-Item ./it/ruby/Gemfile.lock -ErrorAction SilentlyContinue
}
elseif ($language -eq "php") {
    Remove-Item ./it/php/vendor -Recurse -ErrorAction SilentlyContinue
    Remove-Item ./it/php/composer.lock -ErrorAction SilentlyContinue
}

return
