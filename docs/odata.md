---
parent: Welcome to Kiota
---

# OData


## Standard OData query parameters

```csharp
var authProvider = ; /** An authentication provider from the supported language table https://github.com/microsoft/kiota#supported-languages, or your own implementation **/
var requestAdapter = new HttpClientRequestAdapter(authProvider);
var client = new ApiClient(requestAdapter);
var message = await client.Users["bob@contoso.com"]
                          .Events
                          .GetAsync(q => {
                q.Select2 = new List<UserSelectFields> { UserSelectFields.FirstName, UserSelectFields.DisplayName };
                q.Expand2 = UserExpandFields.Calendar;
            } );

```