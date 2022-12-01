# Kiota Web

## Debug tips

```powershell
dotnet watch run
chrome --remote-debugging-port=9222 --user-data-dir="$Env:TEMP\blazor-chrome-debug" https://localhost:7230/
```

Once on the page:
alt + shit + D
