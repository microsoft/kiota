<#
.SYNOPSIS
Closes the newest excess pull requests across Microsoft's Kiota repositories, leaving an explanatory comment.
.DESCRIPTION
Keeps the oldest open pull requests up to Limit, including drafts, across Microsoft
repositories with "kiota" in their names. Requires an authenticated GitHub CLI.
Use -WhatIf to validate the count and preview closures without changing anything.
.EXAMPLE
.\limit-open-pull-requests.ps1 -Username hardikkaurani -WhatIf
.EXAMPLE
.\limit-open-pull-requests.ps1 -Username hardikkaurani -Limit 10
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param (
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-zA-Z0-9][a-zA-Z0-9-]{0,38}$')]
    [string]
    $Username,
    [ValidateRange(0, [int]::MaxValue)]
    [int]
    $Limit = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Get-Command gh -ErrorAction Stop | Out-Null

$response = gh api --hostname github.com --method GET search/issues `
    --raw-field "q=org:microsoft author:$Username is:pr is:open" `
    --raw-field "sort=created" --raw-field "order=asc" --field "per_page=100" `
    --paginate --slurp
if ($LASTEXITCODE -ne 0) {
    throw "Failed to retrieve open pull requests for '$Username' using GitHub CLI."
}

$pages = @($response | ConvertFrom-Json)
foreach ($page in $pages) {
    if ($page.incomplete_results -or $page.total_count -gt 1000) {
        throw "GitHub returned an incomplete search or exceeded its 1,000-result search limit. No pull requests were closed."
    }
}
$allPullRequests = @($pages | ForEach-Object { $_.items })
if ($allPullRequests.Count -ne $pages[0].total_count) {
    throw "The search result count changed or results were truncated. No pull requests were closed. Please retry."
}

$pullRequests = @(
    $allPullRequests |
        Where-Object { ($_.repository_url.Split('/')[-1]) -like '*kiota*' } |
        Sort-Object @{ Expression = { [DateTimeOffset]$_.created_at } }, html_url
)

Write-Host "Found $($pullRequests.Count) open pull request(s) by '$Username' across Microsoft's Kiota repositories. Limit: $Limit."
if ($pullRequests.Count -le $Limit) {
    Write-Host "The count is within the limit. No pull requests need to be closed."
    return
}

$closingComment = @"
Hi @$Username, thank you for your contribution!

This pull request is being closed automatically because you currently have more than $Limit open pull requests across the Kiota repositories. Reviewing takes time, and having this many pull requests open at once overwhelms the reviewers. Please avoid having more than $Limit open pull requests at any given time.

To move forward, you can either:

- Wait for your existing pull requests to get merged, and make sure to respond timely to any feedback to help move things along.
- Close older pull requests that are no longer relevant, and then re-open this one.
"@

$excessPullRequests = @($pullRequests | Select-Object -Skip $Limit)
Write-Host "Keeping the $Limit oldest pull request(s); selected $($excessPullRequests.Count) newer pull request(s) for closure."
foreach ($pullRequest in $excessPullRequests) {
    $openedAt = ([DateTimeOffset]$pullRequest.created_at).ToString('u')
    if ($PSCmdlet.ShouldProcess("$($pullRequest.html_url) (opened $openedAt)", 'Close pull request')) {
        gh pr close $pullRequest.html_url --comment $closingComment
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to close $($pullRequest.html_url). Stopped; any earlier successful closures remain in effect."
        }
        Write-Host "Closed $($pullRequest.html_url)."
    }
}
