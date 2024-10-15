# Step 1: Find all instances of vscode.l10n.t() and extract the strings from .ts and .tsx files
Get-ChildItem -Path src -Recurse -Include *.ts, *.tsx | 
Select-String -Pattern 'vscode.l10n.t\("([^"]+)"\)' | 
ForEach-Object { $_.Matches.Groups[1].Value } | 
Sort-Object | 
Out-File -FilePath "strings.txt"

# Step 2: Check translation files in the l10n folder
$results = @()
foreach ($file in Get-ChildItem -Path "l10n" -Filter bundle.l10n.*.json -Recurse) {
  $translations = Get-Content $file.FullName | 
  Select-String -Pattern '"[^"]+"' | 
  ForEach-Object { $_.Matches.Groups[0].Value.Trim('"') } | 
  Sort-Object

  $missing = Compare-Object (Get-Content "strings.txt") $translations -PassThru | 
  Where-Object { $_.SideIndicator -eq "<=" }

  if ($missing) {
    $untranslatedItems = $missing | ForEach-Object { "<li>$_</li>" }
    $results += [PSCustomObject]@{
      "LanguageFile"        = "$($file.Name) ($($untranslatedItems.Count) found)"
      "UntranslatedStrings" = "<ul>$($untranslatedItems -join "`n")</ul>"
    }
  }
}

# Create the HTML table
$htmlTable = @"
<!DOCTYPE html>
<html>
<head>
<style>
  table {
    width: 100%;
    border-collapse: collapse;
  }
  th, td {
    border: 1px solid black;
    padding: 8px;
    text-align: left;
  }
  th {
    background-color: #f2f2f2;
  }
</style>
</head>
<body>
<h2>Untranslated Strings</h2>
<table>
  <tr>
    <th>Language File</th>
    <th>Untranslated Strings</th>
  </tr>
"@

foreach ($result in $results) {
  $htmlTable += "<tr><td>$($result.LanguageFile)</td><td>$($result.UntranslatedStrings)</td></tr>"
}

$htmlTable += @"
</table>
</body>
</html>
"@

$htmlTable | Out-File -FilePath "untranslated_strings.html"

if ($results.Count -gt 0) {
  Write-Host "Untranslated strings found. See untranslated_strings.html for details." -ForegroundColor Red
  exit 1
}
else {
  Write-Host "All strings have translations." -ForegroundColor Green
}
