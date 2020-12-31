# OData


## Standard OData query parameters

```csharp

var client = new ApiClient();
var message = await client.Users["bob@contoso.com"]
                          .Events
                          .GetAsync(q => {
                q.Select2 = new List<UserSelectFields> { UserSelectFields.FirstName, UserSelectFields.DisplayName };
                q.Expand2 = UserExpandFields.Calendar; 
            } );

```