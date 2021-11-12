---
parent: Welcome to Kiota
---

# OData

## Standard OData query parameters

```csharp
// An authentication provider from the supported language table
// https://github.com/microsoft/kiota#supported-languages, or your own implementation
var authProvider = ;
var coreService = new HttpClientRequestAdapter(authProvider);
var client = new ApiClient(coreService);
var message = await client.Users["bob@contoso.com"]
                          .Events
                          .GetAsync(q => {
                q.Select2 = new List<UserSelectFields> { UserSelectFields.FirstName, UserSelectFields.DisplayName };
                q.Expand2 = UserExpandFields.Calendar;
            } );

```
