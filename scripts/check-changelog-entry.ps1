param (
    [string[]]
    [AllowEmptyCollection()]
    $addedFiles = @()
)

# a pull request has an entry when it adds a file to changelog.d/added or changelog.d/changed
return @($addedFiles | Where-Object { $_ -match "^changelog\.d/(added|changed)/[^/]+\.md$" }).Count -gt 0
